using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商城服务实现（v2.3.0 商城虚拟权益）。
    /// 兑换顺序刻意固定为"全量校验 → 扣分 → 履约"：扣分内部即提交事务，
    /// 因此所有可预见失败（商品下架/库存/越权/非在售）都必须在扣分前拦掉；
    /// 履约阶段异常则原路退分并把订单标 Failed，绝不让用户白花积分
    /// </summary>
    public class MallService : IMallService
    {
        private readonly IMallItemRepository _items;
        private readonly IMallOrderRepository _orders;
        private readonly IMerchantBearingRepository _bearings;
        private readonly IMerchantMemberRepository _members;
        private readonly IPointsService _points;
        private readonly IPointAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        // 改动说明（v2.3.1）：履约失败路径需要 ChangeTracker.Clear 防脏批次外溢到退款
        private readonly OpenFindBearings.Infrastructure.Persistence.Data.ApplicationDbContext _context;
        private readonly ILogger<MallService> _logger;

        public MallService(
            IMallItemRepository items,
            IMallOrderRepository orders,
            IMerchantBearingRepository bearings,
            IMerchantMemberRepository members,
            IPointsService points,
            IPointAccountRepository accounts,
            IUnitOfWork unitOfWork,
            OpenFindBearings.Infrastructure.Persistence.Data.ApplicationDbContext context,
            ILogger<MallService> logger)
        {
            _items = items;
            _orders = orders;
            _bearings = bearings;
            _members = members;
            _points = points;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<MallCatalogResult> GetCatalogAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var items = await _items.GetEnabledAsync(cancellationToken);
            var account = await _accounts.GetByUserIdAsync(userId, cancellationToken);

            var list = items.Select(i => new MallCatalogItem(
                i.Id, i.Key, i.Name, i.Description, i.Icon,
                (int)i.Category, i.EffectivePrice(now),
                i.IsFlashing(now) ? i.PointPrice : null,
                i.IsFlashing(now), i.FlashEnd,
                i.DurationHours, i.Stock, i.SoldCount, !i.HasStock())).ToList();

            return new MallCatalogResult(list, account?.Balance ?? 0);
        }

        /// <inheritdoc/>
        public async Task<RedeemResult> RedeemAsync(Guid userId, Guid itemId, Guid? targetRef, string? requestId,
            CancellationToken cancellationToken = default)
        {
            // 校验一：商品存在且上架
            var item = await _items.GetByIdAsync(itemId, cancellationToken);
            if (item == null || !item.Enabled || !item.IsActive)
                return new RedeemResult(false, "商品不存在或已下架", null, 0, null);

            // 校验二：库存（限量权益兑完即止，稀缺感来自真实库存）
            if (!item.HasStock())
                return new RedeemResult(false, "该权益已兑完", null, 0, null);

            var now = DateTime.UtcNow;
            var price = item.EffectivePrice(now);
            if (price <= 0)
                return new RedeemResult(false, "商品价格配置异常，请联系平台", null, 0, null);

            // 校验三：按类别校验履约目标（扣分前拦掉一切可预见失败）
            MerchantBearing? bearing = null;
            switch (item.Category)
            {
                case MallItemCategory.PinCard:
                    if (!targetRef.HasValue)
                        return new RedeemResult(false, "请选择要置顶的商品", null, 0, null);
                    bearing = await _bearings.GetByIdAsync(targetRef.Value, cancellationToken);
                    if (bearing == null)
                        return new RedeemResult(false, "商品不存在", null, 0, null);
                    // 越权守卫：只能给自己在职商户的商品买曝光
                    var member = await _members.GetActiveByUserAndMerchantAsync(userId, bearing.MerchantId, cancellationToken);
                    if (member == null)
                        return new RedeemResult(false, "只能置顶自己商户的商品", null, 0, null);
                    if (!bearing.IsOnSale)
                        return new RedeemResult(false, "仅在售商品可置顶", null, 0, null);
                    break;

                default:
                    // 寻货次数包/实物礼品履约器未实装：明确拒绝而不是半截生效
                    return new RedeemResult(false, "该权益即将上线，敬请期待", null, 0, null);
            }

            // 扣分（内部提交事务；余额不足由 Debit 抛 InvalidOperationException）
            var bizId = string.IsNullOrWhiteSpace(requestId) ? null : $"mall:{requestId}";
            int deducted;
            try
            {
                deducted = await _points.DeductAsync(userId, PointTransaction.TypeMallRedeem, price, bizId,
                    $"兑换：{item.Name}", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return new RedeemResult(false, "积分不足，去任务中心赚积分吧", null, 0, null);
            }

            if (deducted == 0)
                return new RedeemResult(false, "请勿重复提交", null, 0, null);

            // 履约：建单 + 权益生效 + 扣库存，一次提交
            var order = MallOrder.Create(userId, item, price, targetRef);
            try
            {
                await _orders.AddAsync(order, cancellationToken);

                if (item.Category == MallItemCategory.PinCard && bearing != null)
                {
                    bearing.Pin(now, item.DurationHours ?? 24);
                    await _bearings.UpdateAsync(bearing, cancellationToken);
                    order.MarkFulfilled($"置顶至 {bearing.PinnedUntil:yyyy-MM-dd HH:mm} (UTC)");
                }

                item.ConsumeStock();
                await _items.UpdateAsync(item, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("商城兑换成功: UserId={UserId}, Item={Key}, Points={Points}, Order={OrderId}",
                    userId, item.Key, price, order.Id);

                return new RedeemResult(true, null, order.Id, price, bearing?.PinnedUntil);
            }
            catch (Exception ex)
            {
                // 改动说明（v2.3.1 登录修复同款污染链）：本批失败后 order/bearing/item 仍挂 tracker，
                // 直接退分会让 GrantCore 的 SaveChanges 连带重试这些脏行、二次失败导致积分蒸发。
                // 先 Clear 本请求跟踪（此后除退分外无其他待写），再把订单以 Failed 回挂、与退款流水同批提交
                _logger.LogError(ex, "商城履约失败，执行退分: UserId={UserId}, Item={Key}", userId, item.Key);
                _context.ChangeTracker.Clear();
                order.MarkFailed(ex.Message);
                await _orders.AddAsync(order, cancellationToken);
                await _points.RefundAsync(userId, price, $"mall_refund:{order.Id:N}",
                    $"兑换失败退款：{item.Name}", cancellationToken: cancellationToken);
                return new RedeemResult(false, "兑换失败，积分已退回", null, price, null);
            }
        }

        /// <inheritdoc/>
        public async Task<(List<MallOrderItem> Items, int Total)> GetMyOrdersAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (orders, total) = await _orders.GetByUserPagedAsync(userId, page, pageSize, cancellationToken);
            var items = orders.Select(o => new MallOrderItem(
                o.Id, o.ItemKey, o.ItemName, o.PointsSpent, (int)o.Status, o.Remark, o.CreatedAt, o.FulfilledAt)).ToList();
            return (items, total);
        }
    }
}

using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商城服务实现（v2.3.0 虚拟权益 + v2.4.0 商家挂礼托管）。
    /// 兑换顺序刻意固定为"全量校验 → 扣分 → 履约"：扣分内部即提交事务，
    /// 因此所有可预见失败（商品下架/库存/越权/非在售/防刷闸）都必须在扣分前拦掉；
    /// 履约阶段异常则原路退分并把订单标 Failed，绝不让用户白花积分。
    /// 实物礼品走托管模型：确认收货才结算进发布商户金库（受月顶），争议可退分
    /// </summary>
    public class MallService : IMallService
    {
        /// <summary>实物礼品自动确认收货天数（发货后起算，Admin 无配置入口，产品定死）</summary>
        public const int AutoConfirmDays = 7;

        private readonly IMallItemRepository _items;
        private readonly IMallOrderRepository _orders;
        private readonly IMerchantBearingRepository _bearings;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantRepository _merchants;
        private readonly IPointsService _points;
        private readonly IPointAccountRepository _accounts;
        private readonly IMerchantPointsService _treasury;
        private readonly INotificationService _notifications;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        // 改动说明（v2.3.1）：履约失败路径需要 ChangeTracker.Clear 防脏批次外溢到退款
        private readonly OpenFindBearings.Infrastructure.Persistence.Data.ApplicationDbContext _context;
        private readonly ILogger<MallService> _logger;

        public MallService(
            IMallItemRepository items,
            IMallOrderRepository orders,
            IMerchantBearingRepository bearings,
            IMerchantMemberRepository members,
            IMerchantRepository merchants,
            IPointsService points,
            IPointAccountRepository accounts,
            IMerchantPointsService treasury,
            INotificationService notifications,
            ISystemConfigRepository configs,
            IUnitOfWork unitOfWork,
            OpenFindBearings.Infrastructure.Persistence.Data.ApplicationDbContext context,
            ILogger<MallService> logger)
        {
            _items = items;
            _orders = orders;
            _bearings = bearings;
            _members = members;
            _merchants = merchants;
            _points = points;
            _accounts = accounts;
            _treasury = treasury;
            _notifications = notifications;
            _configs = configs;
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

            // v2.4.0：礼品展示"来自 XX 商家"——按去重后的归属商户批量补名（目录量小，逐个取可接受）
            var ownerNames = new Dictionary<Guid, string?>();
            foreach (var ownerId in items.Where(i => i.OwnerMerchantId.HasValue).Select(i => i.OwnerMerchantId!.Value).Distinct())
            {
                var merchant = await _merchants.GetByIdAsync(ownerId, cancellationToken);
                ownerNames[ownerId] = merchant?.Name;
            }

            var list = items.Select(i => new MallCatalogItem(
                i.Id, i.Key, i.Name, i.Description, i.Icon,
                (int)i.Category, i.EffectivePrice(now),
                i.IsFlashing(now) ? i.PointPrice : null,
                i.IsFlashing(now), i.FlashEnd,
                i.DurationHours, i.Stock, i.SoldCount, !i.HasStock(),
                i.OwnerMerchantId.HasValue && ownerNames.TryGetValue(i.OwnerMerchantId.Value, out var n) ? n : null)).ToList();

            return new MallCatalogResult(list, account?.Balance ?? 0);
        }

        /// <inheritdoc/>
        public async Task<RedeemResult> RedeemAsync(Guid userId, Guid itemId, Guid? targetRef, string? requestId,
            bool useTreasury = false, CancellationToken cancellationToken = default)
        {
            var item = await _items.GetByIdAsync(itemId, cancellationToken);
            if (item == null || !item.Enabled || !item.IsActive)
                return new RedeemResult(false, "商品不存在或已下架", null, 0, null);

            // 实物礼品走独立兑换入口（多收货信息与防刷闸）
            if (item.Category == MallItemCategory.Gift)
                return new RedeemResult(false, "实物礼品请走礼品兑换", null, 0, null);

            if (!item.HasStock())
                return new RedeemResult(false, "该权益已兑完", null, 0, null);

            var now = DateTime.UtcNow;
            var price = item.EffectivePrice(now);
            if (price <= 0)
                return new RedeemResult(false, "商品价格配置异常，请联系平台", null, 0, null);

            if (item.Category != MallItemCategory.PinCard)
                return new RedeemResult(false, "该权益即将上线，敬请期待", null, 0, null);

            // 置顶卡目标校验（扣分前拦掉一切可预见失败）
            if (!targetRef.HasValue)
                return new RedeemResult(false, "请选择要置顶的商品", null, 0, null);
            var bearing = await _bearings.GetByIdAsync(targetRef.Value, cancellationToken);
            if (bearing == null)
                return new RedeemResult(false, "商品不存在", null, 0, null);
            var member = await _members.GetActiveByUserAndMerchantAsync(userId, bearing.MerchantId, cancellationToken);
            if (member == null)
                return new RedeemResult(false, "只能置顶自己商户的商品", null, 0, null);
            if (!bearing.IsOnSale)
                return new RedeemResult(false, "仅在售商品可置顶", null, 0, null);

            var bizId = string.IsNullOrWhiteSpace(requestId) ? null : $"mall:{requestId}";
            if (bizId == null)
                return new RedeemResult(false, "缺少幂等键，请重试", null, 0, null);

            // 支出通道二选一：金库（管理员、同批提交）或个人（内部即提交）
            if (useTreasury)
            {
                if (!member.IsAdmin)
                    return new RedeemResult(false, "仅商户管理员可用金库积分", null, 0, null);
                bool spent;
                try
                {
                    spent = await _treasury.SpendAsync(bearing.MerchantId, price, bizId,
                        $"金库兑换：{item.Name}", cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    return new RedeemResult(false, "商家金库余额不足", null, 0, null);
                }
                if (!spent)
                    return new RedeemResult(false, "请勿重复提交", null, 0, null);
            }
            else
            {
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
            }

            // 履约：建单 + 权益生效 + 扣库存，一次提交（金库支出与订单同批，天然原子）
            var order = MallOrder.Create(userId, item, price, targetRef);
            try
            {
                await _orders.AddAsync(order, cancellationToken);

                bearing.Pin(now, item.DurationHours ?? 24);
                await _bearings.UpdateAsync(bearing, cancellationToken);
                order.MarkFulfilled($"置顶至 {bearing.PinnedUntil:yyyy-MM-dd HH:mm} (UTC)");

                item.ConsumeStock();
                await _items.UpdateAsync(item, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("商城兑换成功: UserId={UserId}, Item={Key}, Points={Points}, Order={OrderId}, Treasury={T}",
                    userId, item.Key, price, order.Id, useTreasury);

                return new RedeemResult(true, null, order.Id, price, bearing.PinnedUntil);
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
                if (useTreasury)
                {
                    // 金库支出已随 Clear 回滚（未提交过），无需退——只有个人扣分路径需要补偿流水
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    await _points.RefundAsync(userId, price, $"mall_refund:{order.Id:N}",
                        $"兑换失败退款：{item.Name}", cancellationToken: cancellationToken);
                }
                return new RedeemResult(false, "兑换失败，积分已退回", null, price, null);
            }
        }

        /// <inheritdoc/>
        public async Task<RedeemResult> RedeemGiftAsync(Guid userId, Guid itemId, string receiverName,
            string receiverPhone, string receiverAddress, string? requestId, CancellationToken cancellationToken = default)
        {
            var item = await _items.GetByIdAsync(itemId, cancellationToken);
            if (item == null || !item.IsMerchantGift || !item.IsGiftSellable)
                return new RedeemResult(false, "礼品不存在或未通过审核", null, 0, null);
            if (!item.HasStock())
                return new RedeemResult(false, "礼品已兑完", null, 0, null);
            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverPhone)
                || string.IsNullOrWhiteSpace(receiverAddress))
                return new RedeemResult(false, "收货人/电话/地址均必填", null, 0, null);

            var now = DateTime.UtcNow;
            var price = item.EffectivePrice(now);
            if (price <= 0)
                return new RedeemResult(false, "礼品价格未定档，请联系平台", null, 0, null);

            // 防刷闸一：自兑排除——本商户任何在职成员（含员工）都不能兑自家礼品
            // （升级自设计稿"仅管理员"：员工也是利益相关方，排除面取宽）
            var ownerId = item.OwnerMerchantId!.Value;
            var selfMember = await _members.GetActiveByUserAndMerchantAsync(userId, ownerId, cancellationToken);
            if (selfMember != null)
                return new RedeemResult(false, "不能兑换自己商户的礼品", null, 0, null);

            // 发布商户必须仍在册经营（Pending/Suspended 商户的礼品不可兑）
            var owner = await _merchants.GetByIdAsync(ownerId, cancellationToken);
            if (owner == null || owner.Status != MerchantStatus.Active)
                return new RedeemResult(false, "礼品所属商家当前不可交易", null, 0, null);

            // 防刷闸二：同址同机月限 3 单（电话或地址任一命中即计数，退款单不占额）
            var monthCap = await GetConfigAsync("Business.GiftReceiverMonthlyLimit", 3, cancellationToken);
            if (monthCap > 0)
            {
                var monthStart = BusinessMonthStartUtc();
                var used = await _orders.CountGiftReceiverSinceAsync(receiverPhone.Trim(), receiverAddress.Trim(), monthStart, cancellationToken);
                if (used >= monthCap)
                    return new RedeemResult(false, $"同一收货信息每月最多兑 {monthCap} 单，下月再来", null, 0, null);
            }

            var bizId = string.IsNullOrWhiteSpace(requestId) ? null : $"mall:{requestId}";
            if (bizId == null)
                return new RedeemResult(false, "缺少幂等键，请重试", null, 0, null);

            // 托管扣分（确认收货才结算给商家；争议可退）
            int deducted;
            try
            {
                deducted = await _points.DeductAsync(userId, PointTransaction.TypeMallRedeem, price, bizId,
                    $"兑换礼品：{item.Name}", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return new RedeemResult(false, "积分不足，去任务中心赚积分吧", null, 0, null);
            }
            if (deducted == 0)
                return new RedeemResult(false, "请勿重复提交", null, 0, null);

            var order = MallOrder.Create(userId, item, price, null,
                receiverName.Trim(), receiverPhone.Trim(), receiverAddress.Trim());
            try
            {
                await _orders.AddAsync(order, cancellationToken);
                order.MarkFulfilled("已托管扣款，等待商家发货");
                item.ConsumeStock();
                await _items.UpdateAsync(item, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 站内信通知商家管理员（发货义务提醒）
                await NotifyMerchantAdminsAsync(ownerId, "礼品被兑换，请及时发货",
                    $"「{item.Name}」被买家兑换，请进入商家-礼品订单填写物流单号。", order.Id, cancellationToken);

                return new RedeemResult(true, null, order.Id, price, null);
            }
            catch (Exception ex)
            {
                // 同 RedeemAsync 失败链：Clear 后订单以 Failed 回挂，与退款流水同批提交
                _logger.LogError(ex, "礼品履约失败，执行退分: UserId={UserId}, Item={Key}", userId, item.Key);
                _context.ChangeTracker.Clear();
                order.MarkFailed(ex.Message);
                await _orders.AddAsync(order, cancellationToken);
                await _points.RefundAsync(userId, price, $"mall_refund:{order.Id:N}",
                    $"礼品兑换失败退款：{item.Name}", cancellationToken: cancellationToken);
                return new RedeemResult(false, "兑换失败，积分已退回", null, price, null);
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Message, int SettledPoints)> ConfirmReceiptAsync(Guid userId, Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var (order, item) = await _orders.GetWithItemAsync(orderId, cancellationToken);
            if (order == null || order.UserId != userId)
                return (false, "订单不存在", 0);
            if (order.ShipStatus == 3)
                return (false, "已确认过收货", 0);
            if (order.ShipStatus != 2 || item == null || !item.IsMerchantGift)
                return (false, "订单当前不可确认收货", 0);

            order.MarkReceived(auto: false);
            await _orders.UpdateAsync(order, cancellationToken);

            // 结算入发布商户金库（同一批提交：收货状态+金库入账原子）
            var settled = item.OwnerMerchantId.HasValue
                ? await _treasury.SettleOrderAsync(order, item.OwnerMerchantId.Value, cancellationToken)
                : 0;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (settled > 0 && item.OwnerMerchantId.HasValue)
            {
                await NotifyMerchantAdminsAsync(item.OwnerMerchantId.Value, "礼品订单已结算",
                    $"「{order.ItemName}」买家确认收货，{settled} 积分已入商家金库。", order.Id, cancellationToken);
            }

            return (true, null, settled);
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Message)> ShipGiftOrderAsync(Guid operatorUserId, Guid orderId,
            string tracking, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tracking))
                return (false, "物流单号必填");

            var (order, item) = await _orders.GetWithItemAsync(orderId, cancellationToken);
            if (order == null || item?.OwnerMerchantId == null)
                return (false, "订单不存在");

            // 归属商户在职成员才可发货（员工可操作，审计留操作人）
            var member = await _members.GetActiveByUserAndMerchantAsync(operatorUserId, item.OwnerMerchantId.Value, cancellationToken);
            if (member == null)
                return (false, "无权处理该订单");
            if (order.ShipStatus != 1)
                return (false, "订单当前不可发货");

            order.MarkShipped(tracking.Trim());
            await _orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notifications.AddInAppAsync(order.UserId, "gift_shipped", "礼品已发货",
                $"「{order.ItemName}」已发货，物流单号 {tracking.Trim()}。确认收货后积分将结算给商家；发货 7 天后自动确认。",
                "mall_order", order.Id, cancellationToken);
            return (true, null);
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Message)> RefundDisputeAsync(Guid orderId, string reason,
            CancellationToken cancellationToken = default)
        {
            var (order, item) = await _orders.GetWithItemAsync(orderId, cancellationToken);
            if (order == null)
                return (false, "订单不存在");
            if (order.ShipStatus == 3)
                return (false, "已收货结算的订单不可原路退分，请走金库侧人工处理");
            if (order.ShipStatus is not (1 or 2) && order.Status != Domain.Enums.MallOrderStatus.Fulfilled)
                return (false, "仅托管中的实物单可争议退款");
            if (order.Status == Domain.Enums.MallOrderStatus.Refunded)
                return (false, "订单已退款");

            order.MarkRefunded(string.IsNullOrWhiteSpace(reason) ? "平台争议处理退款" : reason);
            await _orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 原路退分（退款不查规则表；bizId 与履约失败退款同键，天然互斥幂等）
            await _points.RefundAsync(order.UserId, order.PointsSpent, $"mall_refund:{order.Id:N}",
                $"礼品订单争议退款：{order.ItemName}", cancellationToken: cancellationToken);

            await _notifications.AddInAppAsync(order.UserId, "gift_refunded", "礼品订单已退款",
                $"「{order.ItemName}」订单已由平台处理退款 {order.PointsSpent} 积分。{reason}",
                "mall_order", order.Id, cancellationToken);
            return (true, null);
        }

        /// <inheritdoc/>
        public async Task<(List<MallOrderItem> Items, int Total)> GetMyOrdersAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (orders, total) = await _orders.GetByUserPagedAsync(userId, page, pageSize, cancellationToken);
            var items = orders.Select(o => new MallOrderItem(
                o.Id, o.ItemKey, o.ItemName, o.PointsSpent, (int)o.Status, o.Remark, o.CreatedAt, o.FulfilledAt,
                o.ShipStatus, o.ShipTracking, o.ShippedAt, o.ReceivedAt,
                o.ReceiverName, MaskPhone(o.ReceiverPhone), o.ReceiverAddress)).ToList();
            return (items, total);
        }

        /// <summary>手机号脱敏展示（138****0000），商家侧看全量走订单详情接口按需</summary>
        private static string? MaskPhone(string? phone) =>
            phone is { Length: >= 7 } ? phone[..3] + "****" + phone[^4..] : phone;

        /// <summary>通知商户全体在职管理员（站内信）；单商户管理员数量有限，逐个 await 安全</summary>
        private async Task NotifyMerchantAdminsAsync(Guid merchantId, string title, string body, Guid bizId,
            CancellationToken cancellationToken)
        {
            var admins = await _members.GetActiveByMerchantAsync(merchantId, cancellationToken);
            foreach (var admin in admins.Where(m => m.IsAdmin))
            {
                await _notifications.AddInAppAsync(admin.UserId, "gift_order", title, body, "mall_order", bizId, cancellationToken);
            }
        }

        /// <summary>业务月首（UTC），与金库服务同口径</summary>
        private static DateTime BusinessMonthStartUtc()
        {
            var bNow = BusinessClock.Now;
            var bMonthStart = new DateTime(bNow.Year, bNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return DateTime.SpecifyKind(bMonthStart - BusinessClock.Offset, DateTimeKind.Utc);
        }

        /// <summary>读 Business.* 整型配置（缺失/非法回退默认）</summary>
        private async Task<int> GetConfigAsync(string key, int fallback, CancellationToken ct)
        {
            var parsed = await _configs.GetValueAsync<int?>(key, null);
            return parsed.HasValue && parsed.Value >= 0 ? parsed.Value : fallback;
        }
    }
}

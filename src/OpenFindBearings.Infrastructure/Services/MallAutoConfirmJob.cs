using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 礼品订单自动确认收货 Job（v2.4.0 工会经济）：每小时扫描"发货满 7 天仍未确认"的实物单，
    /// 自动确认并结算进发布商户金库（买家忘点收货的兜底，商家发货义务的真实闭环）。
    /// 商户已退出（非 Active）时只终止订单不再结算——金库已燃烧，防止"释放后再生金"
    /// </summary>
    public class MallAutoConfirmJob : BackgroundService
    {
        /// <summary>发货满该天数未确认即自动收货（与 MallService.AutoConfirmDays 同值）</summary>
        private const int AutoConfirmDays = MallService.AutoConfirmDays;

        /// <summary>扫描间隔：1 小时（时效要求低，与注销冷静期 Job 同节奏）</summary>
        private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(1);

        /// <summary>单轮处理上限（量小防御，正常远达不到）</summary>
        private const int BatchLimit = 100;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MallAutoConfirmJob> _logger;

        public MallAutoConfirmJob(IServiceScopeFactory scopeFactory, ILogger<MallAutoConfirmJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // 单轮失败不终止循环（DB/Identity 瞬时故障下轮重试）
                    _logger.LogError(ex, "礼品自动确认收货 Job 执行失败");
                }
                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        /// <summary>单轮扫描：逐单独立提交（一单失败不拖垮整批）</summary>
        private async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredService<IMallOrderRepository>();
            var items = scope.ServiceProvider.GetRequiredService<IMallItemRepository>();
            var merchants = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
            var treasury = scope.ServiceProvider.GetRequiredService<IMerchantPointsService>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork>();

            var due = await orders.GetAutoConfirmableAsync(DateTime.UtcNow.AddDays(-AutoConfirmDays), BatchLimit, ct);
            if (due.Count == 0)
                return;

            foreach (var order in due)
            {
                try
                {
                    var (fresh, item) = await orders.GetWithItemAsync(order.Id, ct);
                    if (fresh == null || fresh.ShipStatus != 2 || item?.OwnerMerchantId == null)
                        continue; // 期间已被处理

                    fresh.MarkReceived(auto: true);
                    await orders.UpdateAsync(fresh, ct);

                    // 商户已退出（关店/解除归属）：只终止订单不结算（金库已燃烧）
                    var owner = await merchants.GetByIdAsync(item.OwnerMerchantId.Value, ct);
                    var settled = 0;
                    if (owner != null && owner.Status == Domain.Enums.MerchantStatus.Active)
                    {
                        settled = await treasury.SettleOrderAsync(fresh, item.OwnerMerchantId.Value, ct);
                        if (settled > 0)
                        {
                            // 通知全体在职管理员（金库入账可见）
                            var members = scope.ServiceProvider.GetRequiredService<IMerchantMemberRepository>();
                            var admins = await members.GetActiveByMerchantAsync(item.OwnerMerchantId.Value, ct);
                            foreach (var admin in admins.Where(m => m.IsAdmin))
                            {
                                await notifications.AddInAppAsync(admin.UserId, "gift_settlement",
                                    "礼品订单自动结算",
                                    $"「{fresh.ItemName}」发货满 {AutoConfirmDays} 天自动确认，{settled} 积分已入商家金库。",
                                    "mall_order", fresh.Id, ct);
                            }
                        }
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                    _logger.LogInformation("礼品自动确认收货: Order={OrderId}, Settled={Settled}", fresh.Id, settled);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "礼品自动确认收货单条失败: Order={OrderId}", order.Id);
                }
            }
        }
    }
}

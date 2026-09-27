using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家集体任务结算 Job（v2.6.0 M3）：每小时扫描 Active 商户 × 启用任务，
    /// 达标即记台账发奖（不等周期结束——"打 raid 掉落即时发"的体感设计）。
    /// 照 MallAutoConfirmJob 模板：独立 scope、单轮失败不终止循环、逐笔独立提交
    /// </summary>
    public class MerchantTaskSettlementJob : BackgroundService
    {
        /// <summary>扫描间隔：1 小时（集体任务时效要求与自动收货同级）</summary>
        private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MerchantTaskSettlementJob> _logger;

        public MerchantTaskSettlementJob(IServiceScopeFactory scopeFactory, ILogger<MerchantTaskSettlementJob> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IMerchantTaskService>();
                    var settled = await service.RunSettlementSweepAsync(stoppingToken);
                    if (settled > 0)
                        _logger.LogInformation("集体任务结算 Job: 本轮结算 {Count} 笔", settled);
                }
                catch (OperationCanceledException)
                {
                    break; // 停机取消，正常退出
                }
                catch (Exception ex)
                {
                    // 单轮失败不终止循环（DB 瞬时故障下轮重试；台账唯一键保证重跑不双发）
                    _logger.LogError(ex, "集体任务结算 Job 执行失败");
                }
                try
                {
                    await Task.Delay(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}

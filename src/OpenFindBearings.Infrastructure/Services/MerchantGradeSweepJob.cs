using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家牌级每日巡检（v2.19.0 有效期制）：按业务日 03:00（SystemConfig Business.TimeZoneOffsetHours，默认 +8）
    /// 重算全部在职商家，负责三类"时间型"判定——有效期到期续期/降档、在售不足保级到期落普通、
    /// 以及成交滑出有效期后的自然衰减（用户未操作也必须结算）。
    /// 事件驱动的即时升降仍由各业务命令调用 IMerchantGradeService 完成，本任务只兜底时间维度。
    /// </summary>
    public class MerchantGradeSweepJob : BackgroundService
    {
        /// <summary>检查间隔：半小时一次，只为对齐业务日 03:00，不重复执行同一天</summary>
        private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(30);

        /// <summary>巡检触发时刻（业务日小时，按 Business.TimeZoneOffsetHours 换算）</summary>
        private const int SweepHour = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MerchantGradeSweepJob> _logger;
        /// <summary>最近一次已执行的业务日期（yyyy-MM-dd），防止同一天重复整轮重算</summary>
        private string? _lastRunDate;

        /// <summary>构造：注入作用域工厂与日志</summary>
        public MerchantGradeSweepJob(IServiceScopeFactory scopeFactory, ILogger<MerchantGradeSweepJob> logger)
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
                    await TickAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // 失败不终止循环：数据库瞬时不可用下一轮重试
                    _logger.LogError(ex, "商家牌级巡检执行失败");
                }

                try
                {
                    await Task.Delay(TickInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 每半小时检查一次是否到达业务日巡检时刻；到达且当天未跑过时执行整轮重算
        /// </summary>
        private async Task TickAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var configs = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
            var offsetHours = await configs.GetValueAsync<int?>("Business.TimeZoneOffsetHours", null) ?? 8;

            var businessNow = DateTime.UtcNow.AddHours(offsetHours);
            var dateKey = businessNow.ToString("yyyy-MM-dd");
            if (_lastRunDate == dateKey)
                return;

            // 改动说明：除每天 03:00 后触发外，进程首次启动也补跑一次——
            // 停机/发版恰好跨过 03:00 时不会整天无人结算到期牌级
            var shouldRun = _lastRunDate == null || businessNow.Hour >= SweepHour;
            if (!shouldRun)
                return;

            await RunOnceAsync(scope.ServiceProvider, ct);
            _lastRunDate = dateKey;
        }

        /// <summary>
        /// 整轮巡检：逐商户重算（单个失败不阻断整轮），结束时输出汇总日志
        /// </summary>
        private async Task RunOnceAsync(IServiceProvider services, CancellationToken ct)
        {
            var merchants = services.GetRequiredService<IMerchantRepository>();
            var grades = services.GetRequiredService<IMerchantGradeService>();

            var ids = await merchants.GetActiveIdsAsync(ct);
            var failed = 0;
            foreach (var id in ids)
            {
                if (ct.IsCancellationRequested)
                    return;

                try
                {
                    await grades.RecomputeAsync(id, ct);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "商家牌级巡检重算失败: Merchant={MerchantId}", id);
                }
            }

            _logger.LogInformation("商家牌级巡检完成: Total={Total}, Failed={Failed}", ids.Count, failed);
        }
    }
}

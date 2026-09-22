using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 注销冷静期处理 Job（v2.12.0）：每小时扫描注销满 30 天且未匿名化的账户，
    /// 调用 Identity 匿名化（清手机号/邮箱/用户名）后级联清除业务库个人数据并落终态。
    /// 冷静期内账户处于"软删可恢复"状态（重新登录需人工/客服恢复），期满即不可逆。
    /// </summary>
    public class UserDeactivationJob : BackgroundService
    {
        /// <summary>
        /// 冷静期天数（个保法删除义务的落地窗口，主流取 15-30 天）
        /// </summary>
        private const int CoolingOffDays = 30;

        /// <summary>
        /// 扫描间隔：1 小时（注销量小，无需更密；单轮最多处理 100 个防长事务）
        /// </summary>
        private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserDeactivationJob> _logger;

        public UserDeactivationJob(IServiceScopeFactory scopeFactory, ILogger<UserDeactivationJob> logger)
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
                    // 单轮失败不终止循环（Identity 抖动/DB 瞬时故障下轮自愈）
                    _logger.LogError(ex, "注销冷静期 Job 本轮执行失败");
                }
                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        /// <summary>
        /// 单轮处理：取到期用户 → 逐个先 Identity 匿名化成功再清业务库（顺序保证 Identity 失败时
        /// 业务库不动，下轮重试；避免"业务库清了 Identity 还留着手机号"的半匿名状态）
        /// </summary>
        private async Task RunOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

            var due = await userRepository.GetDeactivatedBeforeAsync(DateTime.UtcNow.AddDays(-CoolingOffDays), ct);
            foreach (var user in due)
            {
                if (ct.IsCancellationRequested) break;

                if (!await identityService.AnonymizeUserAsync(user.AuthUserId, ct))
                {
                    _logger.LogWarning("Identity 匿名化失败，保留下轮重试: UserId={UserId}", user.Id);
                    continue;
                }

                await userRepository.AnonymizeCascadeAsync(user, ct);
                _logger.LogInformation("冷静期满已匿名化: UserId={UserId}", user.Id);
            }
        }
    }
}

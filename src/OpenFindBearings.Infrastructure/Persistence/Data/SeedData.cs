using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 数据库种子数据的统一入口，按职责拆分为多个 partial 文件：
    /// Security（权限/角色/绑定）、MasterData（品牌/型号档案/粉丝数）、Demo（开发演示数据）、Config（系统配置键）。
    /// 改动说明：历史上所有种子被 "SystemConfigs 非空即整体跳过" 的单一判据罩住，
    ///   而多个迁移在建库阶段就写入了 SystemConfigs，导致全新库永远得不到权限目录与 Admin 角色，
    ///   后台无人有权限且无法自救。现改为分层幂等：安全目录与主数据每次都校验补全，只有演示数据受开发环境门控。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>
        /// 执行全部种子步骤。各步骤自身幂等，可在每次应用启动时安全重复调用。
        /// </summary>
        /// <param name="provider">服务提供者，用于解析 scoped 的 ApplicationDbContext</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="isDevelopment">是否开发环境，仅影响演示数据是否写入</param>
        public static async Task SeedAsync(IServiceProvider provider, ILogger logger, bool isDevelopment)
        {
            try
            {
                await using var context = provider.GetRequiredService<ApplicationDbContext>();

                // 改动说明：迁移已上移到 Program.cs 的启动块统一执行（失败即抛出、快速暴露），
                //   此处不再重复 MigrateAsync。SeedData 只负责种子数据。
                await EnsureSecurityCatalogAsync(context, logger);

                await EnsureMasterDataAsync(context, logger);

                if (isDevelopment)
                {
                    await EnsureDemoDataAsync(context, logger);
                }

                await EnsureConfigKeysAsync(context);

                logger.LogInformation("数据库种子数据校验完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "数据库种子数据初始化失败");
            }
        }
    }
}
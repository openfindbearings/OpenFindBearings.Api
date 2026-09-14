using MediatR;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Queries.Mobile.CheckVersion;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Mobile.Handlers
{
    /// <summary>
    /// 版本检查查询处理器
    /// </summary>
    public class CheckVersionQueryHandler : IRequestHandler<CheckVersionQuery, VersionCheckResult>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<CheckVersionQueryHandler> _logger;

        public CheckVersionQueryHandler(
            ISystemConfigRepository systemConfigRepository,
            ILogger<CheckVersionQueryHandler> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<VersionCheckResult> Handle(
            CheckVersionQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("检查版本更新: Platform={Platform}, CurrentVersion={CurrentVersion}",
                request.Platform, request.CurrentVersion);

            var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);

            // 获取最新版本（优先使用平台特定版本）
            var latestVersionKey = string.IsNullOrEmpty(request.Platform)
                ? "Mobile.AppVersion"
                : $"Mobile.{request.Platform}.Version";

            var latestVersion = configs.FirstOrDefault(c => c.Key == latestVersionKey)?.Value
                ?? configs.FirstOrDefault(c => c.Key == "Mobile.AppVersion")?.Value
                ?? "1.0.0";

            // 获取最低支持版本
            var minVersionKey = string.IsNullOrEmpty(request.Platform)
                ? "Mobile.MinVersion"
                : $"Mobile.{request.Platform}.MinVersion";

            var minVersion = configs.FirstOrDefault(c => c.Key == minVersionKey)?.Value
                ?? configs.FirstOrDefault(c => c.Key == "Mobile.MinVersion")?.Value
                ?? "1.0.0";

            // 获取强制更新配置
            var forceUpdateKey = string.IsNullOrEmpty(request.Platform)
                ? "Mobile.ForceUpdate"
                : $"Mobile.{request.Platform}.ForceUpdate";

            var forceUpdateConfig = configs.FirstOrDefault(c => c.Key == forceUpdateKey)?.Value
                ?? configs.FirstOrDefault(c => c.Key == "Mobile.ForceUpdate")?.Value
                ?? "false";

            // 获取下载地址
            var downloadUrlKey = string.IsNullOrEmpty(request.Platform)
                ? "Mobile.DownloadUrl"
                : $"Mobile.{request.Platform}.DownloadUrl";

            var downloadUrl = configs.FirstOrDefault(c => c.Key == downloadUrlKey)?.Value
                ?? configs.FirstOrDefault(c => c.Key == "Mobile.DownloadUrl")?.Value
                ?? string.Empty;

            // 获取更新说明
            var updateMessageKey = string.IsNullOrEmpty(request.Platform)
                ? "Mobile.UpdateMessage"
                : $"Mobile.{request.Platform}.UpdateMessage";

            var updateMessage = configs.FirstOrDefault(c => c.Key == updateMessageKey)?.Value
                ?? "发现新版本，建议更新";

            // 改动说明：比较版本改用真 SemVer 语义（NuGet.Versioning）。
            // 原 int.Parse 实现遇 "1.0.0-rc.2" 这类带 prerelease 的规范版本号必抛异常、恒判"无更新"，
            // 现按 SemVer 2.0 规则：先比核心号（1.0.1-rc.1 > 1.0.0-rc.12），核心相同再比 prerelease（rc.12 > rc.1）
            var compareWithLatest = CompareVersions(request.CurrentVersion, latestVersion);
            var hasUpdate = compareWithLatest < 0;
            var compareWithMin = CompareVersions(request.CurrentVersion, minVersion);
            var isForceUpdate = bool.TryParse(forceUpdateConfig, out var force) && force
                && compareWithMin < 0;

            return new VersionCheckResult
            {
                HasUpdate = hasUpdate,
                LatestVersion = latestVersion,
                IsForceUpdate = isForceUpdate,
                UpdateMessage = hasUpdate ? updateMessage : null,
                DownloadUrl = downloadUrl
            };
        }

        /// <summary>
        /// 比较版本号（SemVer 2.0 语义，兼容 v 前缀）
        /// 返回负数表示 v1 &lt; v2，0 表示相等，正数表示 v1 &gt; v2
        /// 改动说明：任一版本串解析失败时退化为"不等即视为需更新"的兜底判断，
        /// 避免旧实现抛异常恒返 0 导致带 rc 后缀的版本永远检测不到更新
        /// </summary>
        /// <param name="v1">客户端上报的当前版本号</param>
        /// <param name="v2">服务端配置的目标版本号</param>
        private int CompareVersions(string v1, string v2)
        {
            var s1 = NormalizeVersion(v1);
            var s2 = NormalizeVersion(v2);

            if (SemanticVersion.TryParse(s1, out var parsed1)
                && SemanticVersion.TryParse(s2, out var parsed2))
            {
                // VersionComparer.Default：忽略 build 元数据，prerelease 低于同号正式版（符合 SemVer）
                return VersionComparer.Default.Compare(parsed1, parsed2);
            }

            _logger.LogWarning("版本号不符合 SemVer 规范，退化为字符串比较: v1={V1}, v2={V2}", v1, v2);
            // 解析失败兜底：不等时视为"客户端落后"（返回 -1 触发更新提示），相等返 0
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase) ? 0 : -1;
        }

        /// <summary>
        /// 版本号归一化：去掉 git tag 风格的 v/V 前缀与首尾空白
        /// </summary>
        /// <param name="version">原始版本串（如 "v1.0.0-rc.1"）</param>
        private static string NormalizeVersion(string version)
        {
            var trimmed = (version ?? string.Empty).Trim();
            return trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V')
                ? trimmed[1..]
                : trimmed;
        }
    }
}

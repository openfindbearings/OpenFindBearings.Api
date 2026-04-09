using MediatR;
using Microsoft.Extensions.Logging;
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

            // 比较版本
            var hasUpdate = CompareVersions(request.CurrentVersion, latestVersion) < 0;
            var isForceUpdate = bool.TryParse(forceUpdateConfig, out var force) && force
                && CompareVersions(request.CurrentVersion, minVersion) < 0;

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
        /// 比较版本号
        /// 返回负数表示 v1 < v2，0 表示相等，正数表示 v1 > v2
        /// </summary>
        private int CompareVersions(string v1, string v2)
        {
            try
            {
                var parts1 = v1.Split('.').Select(int.Parse).ToArray();
                var parts2 = v2.Split('.').Select(int.Parse).ToArray();

                for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
                {
                    var p1 = i < parts1.Length ? parts1[i] : 0;
                    var p2 = i < parts2.Length ? parts2[i] : 0;

                    if (p1 != p2) return p1.CompareTo(p2);
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "版本号比较失败: v1={V1}, v2={V2}", v1, v2);
                return 0; // 出错时认为版本相同
            }
        }
    }
}

namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    /// <summary>
    /// 版本检查结果
    /// </summary>
    public class VersionCheckResult
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public bool IsForceUpdate { get; set; }
        public string? UpdateMessage { get; set; }
        public string? DownloadUrl { get; set; }
    }
}

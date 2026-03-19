namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    /// <summary>
    /// 移动端配置DTO
    /// </summary>
    public class MobileConfigDto
    {
        public string AppVersion { get; set; } = "1.0.0";
        public string MinVersion { get; set; } = "1.0.0";
        public bool ForceUpdate { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Endpoints { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}

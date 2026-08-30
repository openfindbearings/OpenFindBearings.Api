namespace OpenFindBearings.Application.DTOs
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

        /// <summary>站点名称</summary>
        public string SiteName { get; set; } = string.Empty;

        /// <summary>
        /// 站点描述
        /// 改动说明：补充该字段以消费 SystemConfigs 中的 SiteDescription 配置项，
        ///           此前该配置键已入库但无任何读取方，属死配置
        /// </summary>
        public string SiteDescription { get; set; } = string.Empty;

        /// <summary>备案号</summary>
        public string SiteBeiAn { get; set; } = string.Empty;

        /// <summary>客服联系方式</summary>
        public string CustomerService { get; set; } = string.Empty;
    }
}

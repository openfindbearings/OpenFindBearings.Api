namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 移动端配置DTO（BFF ConfigEndpoints 强类型消费后透传 Taro；
    /// 改动说明 v1.5.2 僵尸清理：原 MinVersion/Endpoints/Settings 无任何消费方已删，
    /// 版本比较用的 Mobile.MinVersion 由 CheckVersion 查询直读 SystemConfig 不经本 DTO）
    /// </summary>
    public class MobileConfigDto
    {
        public string AppVersion { get; set; } = "1.0.0";
        public bool ForceUpdate { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;

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

        /// <summary>
        /// 媒体资源公网 base（末尾无斜杠）。
        /// 改动说明：图片改由独立媒体服务直出，前端据此拼相对键；下发以便换域名/切对象存储免发版
        /// </summary>
        public string MediaBaseUrl { get; set; } = string.Empty;
    }
}

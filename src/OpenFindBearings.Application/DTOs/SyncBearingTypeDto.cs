namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步轴承类型DTO
    /// </summary>
    public class SyncBearingTypeDto
    {
        /// <summary>
        /// 类型代码（如 deep_groove_ball）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 类型名称（如深沟球轴承）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 类型描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 数据来源类型
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// 来源站点/系统
        /// </summary>
        public string? SourceSite { get; set; }
    }
}

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步品牌DTO
    /// </summary>
    public class SyncBrandDto
    {
        /// <summary>
        /// 品牌代码（如 SKF、NSK）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Logo URL
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// 国家/地区
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// 品牌档次
        /// </summary>
        public string? Level { get; set; }

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

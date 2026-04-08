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
        /// 品牌描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Logo URL
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// 官方网站
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// 国家/地区
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// 品牌档次
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; } = 0;
    }
}

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 品牌DTO
    /// </summary>
    public class BrandDto
    {
        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 品牌代码（如 SKF）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 所属国家
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// 品牌Logo
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>品牌简介/特点</summary>
        public string? Description { get; set; }
        /// <summary>成立年份</summary>
        public int? FoundedYear { get; set; }
        /// <summary>母公司或所属集团</summary>
        public string? ParentCompany { get; set; }
        /// <summary>官网地址</summary>
        public string? Website { get; set; }
        /// <summary>优势产品线</summary>
        public string? Advantages { get; set; }
        /// <summary>型号后缀体系说明</summary>
        public string? SuffixNotes { get; set; }
        /// <summary>主要应用领域</summary>
        public string? MainApplications { get; set; }

        /// <summary>
        /// 品牌档次
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 轴承数量统计
        /// </summary>
        public int BearingCount { get; set; }
    }
}

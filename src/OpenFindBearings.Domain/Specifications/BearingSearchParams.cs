using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Specifications
{
    /// <summary>
    /// 轴承搜索参数
    /// </summary>
    public class BearingSearchParams
    {
        // ============ 型号搜索 ============

        /// <summary>
        /// 现行代号（模糊匹配）
        /// </summary>
        public string? CurrentCode { get; set; }

        /// <summary>
        /// 曾用代号（模糊匹配）
        /// </summary>
        public string? FormerCode { get; set; }

        /// <summary>
        /// 关键词搜索（同时搜索现行代号、曾用代号、名称、描述）
        /// </summary>
        public string? Keyword { get; set; }

        // ============ 尺寸范围 ============

        /// <summary>
        /// 最小内径 (mm)
        /// </summary>
        public decimal? MinInnerDiameter { get; set; }

        /// <summary>
        /// 最大内径 (mm)
        /// </summary>
        public decimal? MaxInnerDiameter { get; set; }

        /// <summary>
        /// 最小外径 (mm)
        /// </summary>
        public decimal? MinOuterDiameter { get; set; }

        /// <summary>
        /// 最大外径 (mm)
        /// </summary>
        public decimal? MaxOuterDiameter { get; set; }

        /// <summary>
        /// 最小宽度 (mm)
        /// </summary>
        public decimal? MinWidth { get; set; }

        /// <summary>
        /// 最大宽度 (mm)
        /// </summary>
        public decimal? MaxWidth { get; set; }

        // ============ 产地和类别 ============

        /// <summary>
        /// 产地国家
        /// </summary>
        public string? OriginCountry { get; set; }

        /// <summary>
        /// 产品类别（进口/国产/合资）
        /// </summary>
        public BearingCategory? Category { get; set; }

        // ============ 品牌和类型 ============

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// 轴承类型ID（关联字典表）
        /// </summary>
        public Guid? BearingTypeId { get; set; }

        // ============ 其他筛选 ============

        /// <summary>
        /// 是否标准轴承
        /// </summary>
        public bool? IsStandard { get; set; }

        // ============ 排序 ============

        /// <summary>
        /// 排序字段（currentCode, innerDiameter, outerDiameter, width, viewCount）
        /// </summary>
        public string? SortBy { get; set; } = "currentCode";

        /// <summary>
        /// 排序方向（asc, desc）
        /// </summary>
        public string? SortOrder { get; set; } = "asc";

        // ============ 分页 ============

        /// <summary>
        /// 页码，从1开始
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数，默认20，最大100
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 验证并修正参数
        /// </summary>
        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100;

            if (!string.IsNullOrWhiteSpace(SortBy))
            {
                var allowedSortFields = new[] { "currentCode", "innerDiameter", "outerDiameter", "width", "viewCount" };
                if (!allowedSortFields.Contains(SortBy.ToLower()))
                {
                    SortBy = "currentCode";
                }
            }

            if (!string.IsNullOrWhiteSpace(SortOrder))
            {
                SortOrder = SortOrder.ToLower() == "desc" ? "desc" : "asc";
            }
        }
    }
}

namespace OpenFindBearings.Domain.Specifications
{
    public class BearingSearchParams
    {
        // 型号搜索
        public string? PartNumber { get; set; }
        public string? Keyword { get; set; }

        // 内径范围（已有）
        public decimal? MinInnerDiameter { get; set; }
        public decimal? MaxInnerDiameter { get; set; }

        // 新增：外径范围
        public decimal? MinOuterDiameter { get; set; }
        public decimal? MaxOuterDiameter { get; set; }

        // 新增：宽度范围
        public decimal? MinWidth { get; set; }
        public decimal? MaxWidth { get; set; }

        // 新增：品牌和类型筛选
        public Guid? BrandId { get; set; }
        public Guid? BearingTypeId { get; set; }

        // 新增：排序
        public string? SortBy { get; set; } // "partNumber", "innerDiameter", "outerDiameter", "width"
        public string? SortOrder { get; set; } = "asc"; // "asc" 或 "desc"

        // 分页
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

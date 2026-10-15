using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Specifications
{
    public class MerchantSearchParams
    {
        public string? Keyword { get; set; }
        public MerchantType? Type { get; set; }
        public string? City { get; set; }
        public bool? VerifiedOnly { get; set; }
        public bool? IsActive { get; set; }
        public MerchantStatus? Status { get; set; }
        public bool? ExcludeInternet { get; set; }
        // 改动说明：新增排序字段（name=名称 / productcount=在售数 / 空=认证优先默认），供移动端商家结果页排序
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

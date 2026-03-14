using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Specifications
{
    public class MerchantSearchParams
    {
        public string? Keyword { get; set; }
        public MerchantType? Type { get; set; }
        public string? City { get; set; }
        public bool? VerifiedOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

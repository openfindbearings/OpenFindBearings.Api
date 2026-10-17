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
        // 改动说明：新增排序字段（name=名称 / productcount=在售数 / registeredat=注册时间 / 空=认证优先默认），供移动端商家结果页排序
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        /// <summary>注册时间下限（UTC，含）——由上层按业务日历（BusinessClock）换算后传入</summary>
        public DateTime? RegisteredFromUtc { get; set; }

        /// <summary>注册时间上限（UTC，不含）——"含当日"由上层传次日零点实现</summary>
        public DateTime? RegisteredToUtc { get; set; }

        /// <summary>入驻时间下限（UTC，含）——由上层按业务日历（BusinessClock）换算后传入</summary>
        public DateTime? OnboardedFromUtc { get; set; }

        /// <summary>入驻时间上限（UTC，不含）——"含当日"由上层传次日零点实现</summary>
        public DateTime? OnboardedToUtc { get; set; }

        /// <summary>
        /// 来源筛选（v1.50.0 来源拆分）：
        /// true=仅互联网来源（DataSource=Internet）；false=仅非互联网来源；null=不限。
        /// </summary>
        public bool? InternetSource { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

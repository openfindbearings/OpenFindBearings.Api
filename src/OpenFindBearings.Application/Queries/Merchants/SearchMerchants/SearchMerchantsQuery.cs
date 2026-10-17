using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.SearchMerchants
{
    /// <summary>
    /// 搜索商家查询
    /// </summary>
    public record SearchMerchantsQuery : IRequest<PagedResult<MerchantDto>>, IQuery
    {
        /// <summary>
        /// 关键词（搜索名称和公司名）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public MerchantType? Type { get; set; }

        /// <summary>
        /// 是否只显示认证商家
        /// </summary>
        public bool? VerifiedOnly { get; set; }

        /// <summary>
        /// 是否包含已删除商家
        /// </summary>
        public bool? IncludeDeleted { get; set; }

        /// <summary>
        /// 商家状态筛选
        /// </summary>
        public MerchantStatus? Status { get; set; }

        /// <summary>
        /// 是否排除互联网来源商家（仅显示入驻申请商家）
        /// </summary>
        public bool? ExcludeInternet { get; set; }

        /// <summary>
        /// 排序字段（name=名称 / productcount=在售数 / registeredat=注册时间 / 空=认证优先默认）
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// 排序方向（asc/desc）
        /// </summary>
        public string? SortOrder { get; set; }

        /// <summary>
        /// 注册起始日期（业务日历 yyyy-MM-dd，含）——按 BusinessClock 日界换算为 UTC 过滤
        /// </summary>
        public DateOnly? RegisteredFrom { get; set; }

        /// <summary>
        /// 注册截止日期（业务日历 yyyy-MM-dd，含）
        /// </summary>
        public DateOnly? RegisteredTo { get; set; }

        /// <summary>
        /// 入驻起始日期（业务日历 yyyy-MM-dd，含）——按 BusinessClock 日界换算为 UTC 过滤
        /// </summary>
        public DateOnly? OnboardedFrom { get; set; }

        /// <summary>
        /// 入驻截止日期（业务日历 yyyy-MM-dd，含）
        /// </summary>
        public DateOnly? OnboardedTo { get; set; }

        /// <summary>
        /// 来源筛选（v1.50.0 来源拆分）：true=仅互联网来源；false=仅非互联网来源；null=不限
        /// </summary>
        public bool? InternetSource { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

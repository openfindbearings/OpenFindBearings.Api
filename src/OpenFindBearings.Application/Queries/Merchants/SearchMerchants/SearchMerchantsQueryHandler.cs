using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Queries.Merchants.SearchMerchants
{
    /// <summary>
    /// 搜索商家查询处理器
    /// </summary>
    public class SearchMerchantsQueryHandler : IRequestHandler<SearchMerchantsQuery, PagedResult<MerchantDto>>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<SearchMerchantsQueryHandler> _logger;

        public SearchMerchantsQueryHandler(
            IMerchantRepository merchantRepository,
            ILogger<SearchMerchantsQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantDto>> Handle(SearchMerchantsQuery request, CancellationToken cancellationToken)
        {
            var searchParams = new MerchantSearchParams
            {
                Keyword = request.Keyword,
                City = request.City,
                Type = request.Type,
                Status = request.Status,
                VerifiedOnly = request.VerifiedOnly,
                ExcludeInternet = request.ExcludeInternet,
                IsActive = request.IncludeDeleted == true ? null : true,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                // 改动说明（v1.50.0 商家注册时间筛选）：前端传业务日历日期，此处按 BusinessClock 日界换算为 UTC：
                //   下限取当日零点（含）；上限取"次日零点（不含）"以实现"含当日"，全端不做时区自算
                RegisteredFromUtc = request.RegisteredFrom.HasValue
                    ? DateTime.SpecifyKind(request.RegisteredFrom.Value.ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                    : null,
                RegisteredToUtc = request.RegisteredTo.HasValue
                    ? DateTime.SpecifyKind(request.RegisteredTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                    : null,
                // 改动说明（v1.50.0 入驻时间筛选）：同上按业务日历换算，用于"入驻新增"下钻
                OnboardedFromUtc = request.OnboardedFrom.HasValue
                    ? DateTime.SpecifyKind(request.OnboardedFrom.Value.ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                    : null,
                OnboardedToUtc = request.OnboardedTo.HasValue
                    ? DateTime.SpecifyKind(request.OnboardedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - BusinessClock.Offset, DateTimeKind.Utc)
                    : null,
                InternetSource = request.InternetSource,
                Page = request.Page,
                PageSize = request.PageSize
            };

            // 现在 SearchAsync 直接返回 PagedResult<Merchant>
            var result = await _merchantRepository.SearchAsync(searchParams, cancellationToken);

            var items = result.Items.Select(m => m.ToPublicDto()).ToList();

            return new PagedResult<MerchantDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}

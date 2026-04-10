using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;
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
                VerifiedOnly = request.VerifiedOnly,
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

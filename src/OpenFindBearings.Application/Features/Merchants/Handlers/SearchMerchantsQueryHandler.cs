using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
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

            // ✅ 现在 SearchAsync 直接返回 PagedResult<Merchant>
            var result = await _merchantRepository.SearchAsync(searchParams, cancellationToken);

            var items = result.Items.Select(m => new MerchantDto
            {
                Id = m.Id,
                Name = m.Name,
                CompanyName = m.CompanyName,
                Type = m.Type.ToString(),
                ContactPerson = m.Contact?.ContactPerson,
                Phone = m.Contact?.Phone,
                Mobile = m.Contact?.Mobile,
                Email = m.Contact?.Email,
                Address = m.Contact?.Address,
                IsVerified = m.IsVerified,
                Grade = m.Grade.ToString(),
                FollowerCount = m.FollowerCount,
                ProductCount = m.MerchantBearings?.Count ?? 0
            }).ToList();

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

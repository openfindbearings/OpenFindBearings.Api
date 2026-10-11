using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantProducts
{
    /// <summary>
    /// 获取商家产品列表查询处理器
    /// </summary>
    public class GetMerchantProductsQueryHandler : IRequestHandler<GetMerchantProductsQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantProductsQueryHandler> _logger;

        public GetMerchantProductsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantProductsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(GetMerchantProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _merchantBearingRepository.GetByMerchantAsync(request.MerchantId, cancellationToken);

            if (request.OnlyOnSale)
            {
                // v2.15.0 状态统一：在售=审核通过 + 出售中/补货中
                products = products.Where(p => p.ReviewStatus == ListingReviewStatus.Approved
                    && (p.SaleState == SaleState.OnSale || p.SaleState == SaleState.Restocking));
            }

            var totalCount = products.Count();
            var items = products
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => p.ToDto(request.IsAuthenticated))
                .ToList();

            return new PagedResult<MerchantBearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

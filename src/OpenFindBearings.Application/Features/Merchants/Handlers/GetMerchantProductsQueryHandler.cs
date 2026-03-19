using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
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
                products = products.Where(p => p.IsOnSale);
            }

            var totalCount = products.Count();
            var items = products
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new MerchantBearingDto
                {
                    Id = p.Id,
                    MerchantId = p.MerchantId,
                    MerchantName = p.Merchant?.Name ?? string.Empty,
                    BearingId = p.BearingId,
                    BearingPartNumber = p.Bearing?.PartNumber ?? string.Empty,
                    BearingName = p.Bearing?.Name ?? string.Empty,
                    BrandName = p.Bearing?.Brand?.Name,
                    PriceDescription = p.PriceDescription,
                    StockDescription = p.StockDescription,
                    MinOrderDescription = p.MinOrderDescription,
                    Remarks = p.Remarks,
                    IsOnSale = p.IsOnSale,
                    IsFeatured = p.IsFeatured,
                    IsPendingApproval = p.IsPendingApproval,
                    ViewCount = p.ViewCount,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();

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

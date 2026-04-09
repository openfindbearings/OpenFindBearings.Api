using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
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
                    MerchantGrade = p.Merchant?.Grade.ToString() ?? string.Empty,      // ✅ 新增
                    MerchantIsVerified = p.Merchant?.IsVerified ?? false,              // ✅ 新增
                    BearingId = p.BearingId,
                    // ✅ 修改：PartNumber → CurrentCode
                    BearingCurrentCode = p.Bearing?.CurrentCode ?? string.Empty,
                    // ✅ 新增：曾用代号
                    BearingFormerCode = p.Bearing?.FormerCode,
                    BearingName = p.Bearing?.Name ?? string.Empty,
                    // ✅ 新增：轴承类型名称
                    BearingTypeName = p.Bearing?.BearingType,
                    BrandName = p.Bearing?.Brand?.Name,
                    BrandLevel = p.Bearing?.Brand?.Level.ToString(),                   // ✅ 新增
                    Dimensions = p.Bearing != null
                        ? $"{p.Bearing.Dimensions.InnerDiameter}×{p.Bearing.Dimensions.OuterDiameter}×{p.Bearing.Dimensions.Width}"
                        : null,                                                        // ✅ 新增
                    PriceDescription = p.PriceDescription,
                    PriceVisibility = p.PriceVisibility,                               // ✅ 新增
                    NumericPrice = p.NumericPrice,                                     // ✅ 新增
                    StockDescription = p.StockDescription,
                    MinOrderDescription = p.MinOrderDescription,
                    Remarks = p.Remarks,
                    IsOnSale = p.IsOnSale,
                    IsFeatured = p.IsFeatured,
                    IsPendingApproval = p.IsPendingApproval,
                    ViewCount = p.ViewCount,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    // 价格可见性（需要从请求获取登录状态）
                    IsPriceVisible = p.IsPriceVisible(request.IsAuthenticated)        // ✅ 新增
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

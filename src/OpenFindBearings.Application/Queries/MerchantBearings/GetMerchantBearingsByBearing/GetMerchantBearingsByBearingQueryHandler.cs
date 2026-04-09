using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByBearing
{
    /// <summary>
    /// 获取指定轴承的在售商家列表查询处理器
    /// </summary>
    public class GetMerchantBearingsByBearingQueryHandler : IRequestHandler<GetMerchantBearingsByBearingQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingsByBearingQueryHandler> _logger;

        public GetMerchantBearingsByBearingQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingsByBearingQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMerchantBearingsByBearingQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取轴承的在售商家列表: BearingId={BearingId}, Page={Page}, PageSize={PageSize}",
                request.BearingId, request.Page, request.PageSize);

            var merchantBearings = await _merchantBearingRepository.GetByBearingAsync(request.BearingId, cancellationToken);

            // 只返回在售的商品
            merchantBearings = merchantBearings.Where(mb => mb.IsOnSale);

            var totalCount = merchantBearings.Count();
            var items = merchantBearings
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mb => new MerchantBearingDto
                {
                    Id = mb.Id,
                    MerchantId = mb.MerchantId,
                    MerchantName = mb.Merchant?.Name ?? string.Empty,
                    MerchantGrade = mb.Merchant?.Grade.ToString() ?? string.Empty,
                    MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                    BearingId = mb.BearingId,
                    // ✅ 修改：PartNumber → CurrentCode
                    BearingCurrentCode = mb.Bearing?.CurrentCode ?? string.Empty,
                    // ✅ 新增：曾用代号
                    BearingFormerCode = mb.Bearing?.FormerCode,
                    BearingName = mb.Bearing?.Name ?? string.Empty,
                    // ✅ 新增：轴承类型名称
                    BearingTypeName = mb.Bearing?.BearingType,
                    BrandName = mb.Bearing?.Brand?.Name,
                    BrandLevel = mb.Bearing?.Brand?.Level.ToString(),
                    Dimensions = mb.Bearing != null
                        ? $"{mb.Bearing.Dimensions.InnerDiameter}×{mb.Bearing.Dimensions.OuterDiameter}×{mb.Bearing.Dimensions.Width}"
                        : null,
                    PriceDescription = mb.PriceDescription,
                    PriceVisibility = mb.PriceVisibility,
                    NumericPrice = mb.NumericPrice,
                    StockDescription = mb.StockDescription,
                    MinOrderDescription = mb.MinOrderDescription,
                    Remarks = mb.Remarks,
                    IsOnSale = mb.IsOnSale,
                    IsFeatured = mb.IsFeatured,
                    IsPendingApproval = mb.IsPendingApproval,
                    ViewCount = mb.ViewCount,
                    CreatedAt = mb.CreatedAt,
                    UpdatedAt = mb.UpdatedAt,
                    IsPriceVisible = mb.IsPriceVisible(request.IsAuthenticated)
                })
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

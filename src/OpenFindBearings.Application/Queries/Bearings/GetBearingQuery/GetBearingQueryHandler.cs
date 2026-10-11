using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingQuery
{
    public class GetBearingQueryHandler : IRequestHandler<GetBearingQuery, BearingDetailDto?>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<GetBearingQueryHandler> _logger;

        public GetBearingQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IBearingInterchangeRepository interchangeRepository,
            IMediator mediator,
            ILogger<GetBearingQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _interchangeRepository = interchangeRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<BearingDetailDto?> Handle(GetBearingQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取轴承详情: BearingId={BearingId}", request.Id);

            var bearing = await _bearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearing == null)
                return null;

            // 发布浏览次数增加事件
            bearing.IncrementViewCount(request.UserId, request.SessionId);
            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            // 获取在售商家列表
            var merchantBearings = await _merchantBearingRepository.GetByBearingAsync(bearing.Id, cancellationToken);
            // v2.15.0 状态统一：在售商家 = 审核通过 + 出售中/补货中
            var onSaleMerchants = merchantBearings
                .Where(mb => mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking))
                .Select(mb => new MerchantBearingDto
                {
                    Id = mb.Id,
                    MerchantId = mb.MerchantId,
                    MerchantName = mb.Merchant?.Name ?? string.Empty,
                    MerchantGrade = mb.Merchant?.Grade.ToString() ?? string.Empty,
                    MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                    BearingId = mb.BearingId,
                    PriceDescription = mb.PriceDescription,
                    PriceVisibility = mb.PriceVisibility,
                    NumericPrice = mb.NumericPrice,
                    StockDescription = mb.StockDescription,
                    MinOrderDescription = mb.MinOrderDescription,
                    Remarks = mb.Remarks,
                    SaleState = mb.SaleState,
                    ReviewStatus = mb.ReviewStatus,
                    ViewCount = mb.ViewCount,
                    CreatedAt = mb.CreatedAt,
                    UpdatedAt = mb.UpdatedAt
                })
                // 出售中优先，再按价格（原 IsFeatured 排序已废弃）
                .OrderByDescending(mb => mb.SaleState == SaleState.OnSale)
                .ThenBy(mb => mb.NumericPrice)
                .ToList();

            // 获取替代品列表
            var interchanges = await _interchangeRepository.GetBySourceBearingAsync(bearing.Id, cancellationToken);
            var interchangeBearings = interchanges
                .Where(i => i.TargetBearing != null && i.TargetBearing.IsActive)
                .Select(i => i.TargetBearing!.ToDto())
                .ToList();

            return new BearingDetailDto
            {
                Id = bearing.Id,
                PartNumber = bearing.PartNumber,
                Description = bearing.Description,
                InnerDiameter = bearing.Dimensions.InnerDiameter,
                OuterDiameter = bearing.Dimensions.OuterDiameter,
                Width = bearing.Dimensions.Width,
                Weight = bearing.Weight,
                BrandId = bearing.BrandId,
                BrandName = bearing.Brand?.Name ?? string.Empty,
                BearingTypeId = bearing.BearingTypeId,
                BearingTypeName = bearing.BearingType,
                PrecisionGrade = bearing.PrecisionGrade,
                Material = bearing.Material,
                SealType = bearing.SealType,
                CageType = bearing.CageType,
                DynamicLoad = bearing.Performance?.DynamicLoad,
                StaticLoad = bearing.Performance?.StaticLoad,
                LimitingSpeed = bearing.Performance?.LimitingSpeed,
                LimitingSpeedGrease = bearing.Performance?.LimitingSpeedGrease,
                LimitingSpeedOil = bearing.Performance?.LimitingSpeedOil,
                ViewCount = bearing.ViewCount,
                Merchants = onSaleMerchants,
                Interchanges = interchangeBearings,
                OriginCountry = bearing.OriginCountry,
                Category = bearing.Category.ToString(),
                IsStandard = bearing.IsStandard,
                StructureType = bearing.StructureType,
                SizeSeries = bearing.SizeSeries,
                ChamferRmin = bearing.ChamferRmin,
                ChamferRmax = bearing.ChamferRmax,
                Trademark = bearing.Trademark,
                Image3DUrl = bearing.Image3DUrl,
                Image2DUrl = bearing.Image2DUrl
            };
        }
    }
}

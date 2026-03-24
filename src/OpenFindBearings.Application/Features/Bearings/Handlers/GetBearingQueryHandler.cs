using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 获取轴承查询处理器
    /// </summary>
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

            // 发布浏览次数增加事件（异步处理）
            bearing.IncrementViewCount(request.UserId, request.SessionId);
            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            // 获取在售商家列表
            var merchantBearings = await _merchantBearingRepository.GetByBearingAsync(bearing.Id, cancellationToken);
            var onSaleMerchants = merchantBearings
                .Where(mb => mb.IsOnSale)
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
                    IsOnSale = mb.IsOnSale,
                    IsFeatured = mb.IsFeatured,
                    ViewCount = mb.ViewCount,
                    CreatedAt = mb.CreatedAt,
                    UpdatedAt = mb.UpdatedAt
                })
                .OrderByDescending(mb => mb.IsFeatured)
                .ThenBy(mb => mb.NumericPrice)
                .ToList();

            // 获取替代品列表
            var interchanges = await _interchangeRepository.GetBySourceBearingAsync(bearing.Id, cancellationToken);
            var interchangeBearings = interchanges
                .Where(i => i.TargetBearing != null && i.TargetBearing.IsActive)
                .Select(i => new BearingDto
                {
                    Id = i.TargetBearing!.Id,
                    PartNumber = i.TargetBearing.PartNumber,
                    Name = i.TargetBearing.Name,
                    InnerDiameter = i.TargetBearing.Dimensions.InnerDiameter,
                    OuterDiameter = i.TargetBearing.Dimensions.OuterDiameter,
                    Width = i.TargetBearing.Dimensions.Width,
                    BrandId = i.TargetBearing.BrandId,
                    BrandName = i.TargetBearing.Brand?.Name ?? string.Empty,
                    BearingTypeId = i.TargetBearing.BearingTypeId,
                    BearingTypeName = i.TargetBearing.BearingType?.Name ?? string.Empty,
                    ViewCount = i.TargetBearing.ViewCount,
                    FavoriteCount = i.TargetBearing.FavoriteCount
                })
                .ToList();

            return new BearingDetailDto
            {
                Id = bearing.Id,
                PartNumber = bearing.PartNumber,
                Name = bearing.Name,
                Description = bearing.Description,
                InnerDiameter = bearing.Dimensions.InnerDiameter,
                OuterDiameter = bearing.Dimensions.OuterDiameter,
                Width = bearing.Dimensions.Width,
                Weight = bearing.Weight,
                BrandId = bearing.BrandId,
                BrandName = bearing.Brand?.Name ?? string.Empty,
                BearingTypeId = bearing.BearingTypeId,
                BearingTypeName = bearing.BearingType?.Name ?? string.Empty,
                PrecisionGrade = bearing.PrecisionGrade,
                Material = bearing.Material,
                SealType = bearing.SealType,
                CageType = bearing.CageType,
                DynamicLoadRating = bearing.Performance?.DynamicLoadRating,
                StaticLoadRating = bearing.Performance?.StaticLoadRating,
                LimitingSpeed = bearing.Performance?.LimitingSpeed,
                ViewCount = bearing.ViewCount,
                Merchants = onSaleMerchants,
                Interchanges = interchangeBearings,
                OriginCountry = bearing.OriginCountry,
                Category = bearing.Category.ToString()
            };
        }
    }
}

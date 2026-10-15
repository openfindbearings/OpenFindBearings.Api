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
                // 改动说明（v2.13.2 映射收敛）：原内联 new MerchantBearingDto 只填子集，漏 RestockEta/Dimensions/
                //   BearingPartNumber/BrandName/图片等，与唯一映射 BearingExtensions.ToPublicDto 分歧；改走统一扩展
                .Select(mb => mb.ToPublicDto())
                // 出售中优先，再按价格（原 IsFeatured 排序已废弃）
                .OrderByDescending(mb => mb.SaleState == SaleState.OnSale)
                .ThenBy(mb => mb.NumericPrice)
                .ToList();

            // 获取替代品列表（v2.16.0：仅已生效，源∪双向反向；含三级类型）
            var interchanges = await _interchangeRepository.GetApprovedForBearingAsync(bearing.Id, cancellationToken);
            var interchangeItems = interchanges
                .Select(i => i.ToInterchangeDto(bearing.Id))
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
                // 改动说明（v2.13.2 映射对齐）：详情此前只填 BearingTypeName，BearingType 落空串，
                //   与列表映射（BearingExtensions.ToDto 两者同填）不一致，前端详情页类型显示为空
                BearingType = bearing.BearingType,
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
                Interchanges = interchangeItems,
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

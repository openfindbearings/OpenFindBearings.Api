using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 获取轴承查询处理器
    /// </summary>
    public class GetBearingQueryHandler : IRequestHandler<GetBearingQuery, BearingDetailDto?>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingQueryHandler> _logger;

        public GetBearingQueryHandler(
            IBearingRepository bearingRepository,
            ILogger<GetBearingQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BearingDetailDto?> Handle(GetBearingQuery request, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearing == null)
                return null;

            // TODO: 增加浏览次数
            // bearing.IncrementViewCount();

            return MapToDto(bearing);
        }

        private BearingDetailDto MapToDto(Domain.Entities.Bearing bearing)
        {
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
                Merchants = new List<MerchantBearingDto>() // TODO: 填充在售商家
            };
        }
    }
}

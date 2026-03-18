using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 通过型号获取轴承查询处理器
    /// </summary>
    public class GetBearingByPartNumberQueryHandler : IRequestHandler<GetBearingByPartNumberQuery, BearingDetailDto?>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingByPartNumberQueryHandler> _logger;

        public GetBearingByPartNumberQueryHandler(
            IBearingRepository bearingRepository,
            ILogger<GetBearingByPartNumberQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BearingDetailDto?> Handle(GetBearingByPartNumberQuery request, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByPartNumberAsync(request.PartNumber, cancellationToken);
            if (bearing == null)
                return null;

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
                ViewCount = bearing.ViewCount
            };
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingByCode
{
    public class GetBearingByCodeQueryHandler : IRequestHandler<GetBearingByCodeQuery, BearingDetailDto?>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingByCodeQueryHandler> _logger;

        public GetBearingByCodeQueryHandler(
            IBearingRepository bearingRepository,
            ILogger<GetBearingByCodeQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BearingDetailDto?> Handle(GetBearingByCodeQuery request, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByPartNumberAsync(request.CurrentCode, cancellationToken);
            if (bearing == null)
                return null;

            return MapToDto(bearing);
        }

        private BearingDetailDto MapToDto(Bearing bearing)
        {
            return new BearingDetailDto
            {
                Id = bearing.Id,
                CurrentCode = bearing.CurrentCode,
                FormerCode = bearing.FormerCode,          
                Name = bearing.Name,
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
                DynamicLoadRating = bearing.Performance?.DynamicLoadRating,
                StaticLoadRating = bearing.Performance?.StaticLoadRating,
                LimitingSpeed = bearing.Performance?.LimitingSpeed,
                ViewCount = bearing.ViewCount,
                OriginCountry = bearing.OriginCountry,
                Category = bearing.Category.ToString(),
                IsStandard = bearing.IsStandard,              
                StructureType = bearing.StructureType,        
                SizeSeries = bearing.SizeSeries,              
                ChamferRmin = bearing.ChamferRmin,            
                ChamferRmax = bearing.ChamferRmax,            
                Trademark = bearing.Trademark                 
            };
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
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
            var bearing = await _bearingRepository.GetByPartNumberAsync(request.PartNumber, cancellationToken);
            if (bearing == null)
                return null;

            var dto = bearing.ToDto() as BearingDetailDto;
            if (dto != null)
            {
                dto.Description = bearing.Description;
                dto.PrecisionGrade = bearing.PrecisionGrade;
                dto.Material = bearing.Material;
                dto.SealType = bearing.SealType;
                dto.CageType = bearing.CageType;
                dto.DynamicLoad = bearing.Performance?.DynamicLoad;
                dto.StaticLoad = bearing.Performance?.StaticLoad;
                dto.LimitingSpeed = bearing.Performance?.LimitingSpeed;
                dto.LimitingSpeedGrease = bearing.Performance?.LimitingSpeedGrease;
                dto.LimitingSpeedOil = bearing.Performance?.LimitingSpeedOil;
                dto.StructureType = bearing.StructureType;
                dto.SizeSeries = bearing.SizeSeries;
                dto.ChamferRmin = bearing.ChamferRmin;
                dto.ChamferRmax = bearing.ChamferRmax;
                dto.Trademark = bearing.Trademark;
                dto.Image3DUrl = bearing.Image3DUrl;
                dto.Image2DUrl = bearing.Image2DUrl;
            }
            return dto;
        }
    }
}

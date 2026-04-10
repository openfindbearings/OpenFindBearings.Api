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
            var bearing = await _bearingRepository.GetByPartNumberAsync(request.CurrentCode, cancellationToken);
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
                dto.DynamicLoadRating = bearing.Performance?.DynamicLoadRating;
                dto.StaticLoadRating = bearing.Performance?.StaticLoadRating;
                dto.LimitingSpeed = bearing.Performance?.LimitingSpeed;
                dto.StructureType = bearing.StructureType;
                dto.SizeSeries = bearing.SizeSeries;
                dto.ChamferRmin = bearing.ChamferRmin;
                dto.ChamferRmax = bearing.ChamferRmax;
                dto.Trademark = bearing.Trademark;
            }
            return dto;
        }
    }
}

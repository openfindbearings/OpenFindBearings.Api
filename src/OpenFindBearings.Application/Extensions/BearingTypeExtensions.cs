using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class BearingTypeExtensions
    {
        public static BearingTypeDto ToDto(this BearingType bearingType, int? bearingCount = null)
        {
            return new BearingTypeDto
            {
                Id = bearingType.Id,
                Code = bearingType.Code,
                Name = bearingType.Name,
                Description = bearingType.Description,
                BearingCount = bearingCount ?? 0
            };
        }
    }
}
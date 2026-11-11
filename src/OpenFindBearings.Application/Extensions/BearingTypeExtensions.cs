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
                // 改动说明（类型资料完善）：透出新增的可编辑资料字段
                EnglishName = bearingType.EnglishName,
                StructureFeatures = bearingType.StructureFeatures,
                LoadCharacteristics = bearingType.LoadCharacteristics,
                TypicalPrefixes = bearingType.TypicalPrefixes,
                MainApplications = bearingType.MainApplications,
                IsSeparable = bearingType.IsSeparable,
                ImageUrl = bearingType.ImageUrl,
                BearingCount = bearingCount ?? 0
            };
        }
    }
}
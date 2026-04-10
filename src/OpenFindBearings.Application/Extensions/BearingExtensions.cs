using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;

namespace OpenFindBearings.Application.Extensions
{
    public static class BearingExtensions
    {
        public static BearingDto ToDto(this Bearing bearing)
        {
            return new BearingDto
            {
                Id = bearing.Id,
                CurrentCode = bearing.CurrentCode,
                FormerCode = bearing.FormerCode,
                Name = bearing.Name,
                Description = bearing.Description,
                BearingType = bearing.BearingType,
                InnerDiameter = bearing.Dimensions.InnerDiameter,
                OuterDiameter = bearing.Dimensions.OuterDiameter,
                Width = bearing.Dimensions.Width,
                Weight = bearing.Weight,
                BrandId = bearing.BrandId,
                BrandName = bearing.Brand?.Name ?? string.Empty,
                BrandCountry = bearing.Brand?.Country,
                BearingTypeId = bearing.BearingTypeId,
                BearingTypeName = bearing.BearingType,
                OriginCountry = bearing.OriginCountry,
                Category = bearing.Category.ToString(),
                IsStandard = bearing.IsStandard,
                ViewCount = bearing.ViewCount,
                FavoriteCount = bearing.FavoriteCount
            };
        }
    }
}
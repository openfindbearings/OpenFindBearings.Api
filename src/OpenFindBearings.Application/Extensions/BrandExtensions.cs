using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class BrandExtensions
    {
        public static BrandDto ToDto(this Brand brand, int? bearingCount = null)
        {
            return new BrandDto
            {
                Id = brand.Id,
                Code = brand.Code,
                Name = brand.Name,
                Country = brand.Country,
                LogoUrl = brand.LogoUrl,
                Level = brand.Level.ToString(),
                BearingCount = bearingCount ?? 0
            };
        }
    }
}
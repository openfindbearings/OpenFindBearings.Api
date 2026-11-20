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
                // 改动说明（2026-11-20 拼音检索）：直接读实体上预计算的检索键（与商户同口径）
                SearchPinyin = brand.SearchPinyin ?? string.Empty,
                SearchInitials = brand.SearchInitials ?? string.Empty,
                SearchPinyinFuzzy = brand.SearchPinyinFuzzy ?? string.Empty,
                Country = brand.Country,
                LogoUrl = brand.LogoUrl,
                // 改动说明（品牌资料完善）：透出新增的可编辑资料字段
                Description = brand.Description,
                FoundedYear = brand.FoundedYear,
                ParentCompany = brand.ParentCompany,
                Website = brand.Website,
                Advantages = brand.Advantages,
                SuffixNotes = brand.SuffixNotes,
                MainApplications = brand.MainApplications,
                Level = brand.Level.ToString(),
                BearingCount = bearingCount ?? 0
            };
        }
    }
}
using OpenFindBearings.Application.Features.Admin.DTOs;

namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    /// <summary>
    /// 移动端首页DTO
    /// </summary>
    public class MobileHomeDto
    {
        public List<BannerDto> Banners { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public List<MobileBearingLightDto> HotBearings { get; set; } = new();
        public List<MobileBearingLightDto> RecommendedBearings { get; set; } = new();
        public List<MerchantSimpleDto> RecommendedMerchants { get; set; } = new();
        public UserStatsDto? UserStats { get; set; }
    }
}

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 移动端首页DTO
    /// </summary>
    public class MobileHomeDto
    {
        public List<MobileBannerLightDto> Banners { get; set; } = new();
        public List<MobileCategoryLightDto> Categories { get; set; } = new();
        public List<MobileBearingLightDto> HotBearings { get; set; } = new();
        public List<MobileBearingLightDto> RecommendedBearings { get; set; } = new();
        public List<MerchantLightDto> RecommendedMerchants { get; set; } = new();
        public MobileUserStatsLightDto? UserStats { get; set; }
    }
}

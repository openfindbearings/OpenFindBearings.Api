using OpenFindBearings.Application.Features.MerchantBearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.DTOs
{
    /// <summary>
    /// 轴承详情DTO
    /// </summary>
    public class BearingDetailDto : BearingDto
    {
        // 技术参数
        public string? PrecisionGrade { get; set; }
        public string? Material { get; set; }
        public string? SealType { get; set; }
        public string? CageType { get; set; }

        // 性能参数
        public decimal? DynamicLoadRating { get; set; }
        public decimal? StaticLoadRating { get; set; }
        public decimal? LimitingSpeed { get; set; }

        // 在售商家列表
        public List<MerchantBearingDto> Merchants { get; set; } = new();

        // 替代品列表
        public List<BearingDto> Interchanges { get; set; } = new();
    }
}

namespace OpenFindBearings.Application.DTOs
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

        // 结构信息
        public string? StructureType { get; set; }
        public string? SizeSeries { get; set; }

        // 倒角尺寸
        public decimal? ChamferRmin { get; set; }
        public decimal? ChamferRmax { get; set; }

        // 商标
        public string? Trademark { get; set; }

        // 数据来源
        public string? DataSourceType { get; set; }
        public int? ReliabilityScore { get; set; }
        public bool IsDataVerified { get; set; }

        // 在售商家列表
        public List<MerchantBearingDto> Merchants { get; set; } = [];

        // 替代品列表
        public List<BearingDto> Interchanges { get; set; } = [];
    }
}

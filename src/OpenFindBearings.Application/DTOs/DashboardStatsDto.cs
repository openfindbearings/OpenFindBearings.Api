namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 管理后台仪表盘统计DTO
    /// </summary>
    public class DashboardStatsDto
    {
        /// <summary>
        /// 统计时间
        /// </summary>
        public DateTime StatsTime { get; set; }

        /// <summary>
        /// 轴承统计
        /// </summary>
        public BearingStatsDto Bearings { get; set; } = new();

        /// <summary>
        /// 商家统计
        /// </summary>
        public MerchantStatsDto Merchants { get; set; } = new();

        /// <summary>
        /// 用户统计
        /// </summary>
        public UserStatsDto Users { get; set; } = new();

        /// <summary>
        /// 纠错统计
        /// </summary>
        public CorrectionStatsDto Corrections { get; set; } = new();

        /// <summary>
        /// 待审核事项
        /// </summary>
        public PendingStatsDto Pending { get; set; } = new();
    }

    /// <summary>
    /// 轴承统计
    /// </summary>
    public class BearingStatsDto
    {
        public int TotalCount { get; set; }
        public int TodayAdded { get; set; }
        public int ThisWeekAdded { get; set; }
        public int ThisMonthAdded { get; set; }
        public List<BrandDistributionDto> TopBrands { get; set; } = new();
        public List<TypeDistributionDto> TopTypes { get; set; } = new();
    }

    /// <summary>
    /// 商家统计
    /// </summary>
    public class MerchantStatsDto
    {
        public int TotalCount { get; set; }
        public int VerifiedCount { get; set; }
        public int PendingVerification { get; set; }
        public int TodayRegistered { get; set; }
        public List<MerchantTypeDistributionDto> TypeDistribution { get; set; } = new();
    }

    /// <summary>
    /// 用户统计
    /// </summary>
    public class UserStatsDto
    {
        public int TotalCount { get; set; }
        public int AdminCount { get; set; }
        public int MerchantStaffCount { get; set; }
        public int IndividualCount { get; set; }
        public int TodayRegistered { get; set; }
        public int ActiveToday { get; set; }
    }

    /// <summary>
    /// 纠错统计
    /// </summary>
    public class CorrectionStatsDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int TodaySubmitted { get; set; }
    }

    /// <summary>
    /// 待审核统计
    /// </summary>
    public class PendingStatsDto
    {
        public int PendingMerchantBearings { get; set; }
        public int PendingCorrections { get; set; }
        public int PendingMerchantVerifications { get; set; }
    }

    /// <summary>
    /// 品牌分布
    /// </summary>
    public class BrandDistributionDto
    {
        public string BrandName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// 类型分布
    /// </summary>
    public class TypeDistributionDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// 商家类型分布
    /// </summary>
    public class MerchantTypeDistributionDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

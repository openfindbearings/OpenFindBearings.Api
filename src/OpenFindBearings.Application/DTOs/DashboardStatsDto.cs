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
        /// 品牌统计
        /// </summary>
        public BrandStatsDto Brands { get; set; } = new();

        /// <summary>
        /// 类型统计
        /// </summary>
        public TypeStatsDto Types { get; set; } = new();

        /// <summary>
        /// 商家统计
        /// </summary>
        public MerchantStatsDto Merchants { get; set; } = new();

        /// <summary>
        /// 替代品统计（v1.50.0：替代关系新增按来源拆分）
        /// </summary>
        public InterchangesStatsDto Interchanges { get; set; } = new();

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

        /// <summary>
        /// 统计周期起点（业务日历 yyyy-MM-dd），供前端把图表柱子下钻到明细列表时带上筛选起止
        /// </summary>
        public StatsPeriodsDto Periods { get; set; } = new();
    }

    /// <summary>
    /// 统计周期起点（业务日历日期，yyyy-MM-dd；日界口径与统计一致，均取 BusinessClock）
    /// </summary>
    public class StatsPeriodsDto
    {
        /// <summary>今日（业务日历）</summary>
        public string Today { get; set; } = string.Empty;

        /// <summary>本周起点：本周一（业务日历）</summary>
        public string WeekStart { get; set; } = string.Empty;

        /// <summary>本月起点：本月 1 日（业务日历）</summary>
        public string MonthStart { get; set; } = string.Empty;
    }

    /// <summary>
    /// 轴承统计
    /// </summary>
    public class BearingStatsDto
    {
        public int TotalCount { get; set; }

        /// <summary>改动说明（v1.50.0 来源拆分）：互联网来源新增（爬虫/Sync），按今日/本周/本月</summary>
        public PeriodCountDto InternetAdded { get; set; } = new();

        /// <summary>改动说明（v1.50.0 来源拆分）：非互联网来源新增（平台手工/Excel/OCR 等），按今日/本周/本月</summary>
        public PeriodCountDto ManualAdded { get; set; } = new();

        public List<BrandDistributionDto> TopBrands { get; set; } = new();
        public List<TypeDistributionDto> TopTypes { get; set; } = new();
    }

    /// <summary>
    /// 品牌统计
    /// </summary>
    public class BrandStatsDto
    {
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 类型统计
    /// </summary>
    public class TypeStatsDto
    {
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 商家统计
    /// </summary>
    public class MerchantStatsDto
    {
        public int TotalCount { get; set; }
        public int VerifiedCount { get; set; }

        /// <summary>改动说明（v1.50.0 入驻统计）：已入驻商户数（OnboardedAt 非空），仪表盘卡片"已入驻"真值</summary>
        public int OnboardedCount { get; set; }

        public int PendingApplicationCount { get; set; }

        /// <summary>互联网来源新增（同步进来的商户），按今日/本周/本月</summary>
        public PeriodCountDto InternetAdded { get; set; } = new();

        /// <summary>入驻新增（OnboardedAt 口径，含"认领"既有互联网商户），按今日/本周/本月</summary>
        public PeriodCountDto Onboarded { get; set; } = new();

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
        public int PendingDocuments { get; set; }
    }

    /// <summary>
    /// 按周期计数（今日/本周/本月），配合业务日历起点使用
    /// </summary>
    public class PeriodCountDto
    {
        /// <summary>今日（业务日历）</summary>
        public int Today { get; set; }

        /// <summary>本周（自本周一业务日历起点）</summary>
        public int Week { get; set; }

        /// <summary>本月（自本月 1 日业务日历起点）</summary>
        public int Month { get; set; }
    }

    /// <summary>
    /// 替代品（替代关系）统计
    /// </summary>
    public class InterchangesStatsDto
    {
        public int TotalCount { get; set; }

        /// <summary>待审核候选数（商家报候选/平台提交后未生效）</summary>
        public int PendingCount { get; set; }

        /// <summary>互联网来源新增（Sync 批量），按今日/本周/本月</summary>
        public PeriodCountDto InternetAdded { get; set; } = new();

        /// <summary>人工来源新增（平台维护 + 商家报候选），按今日/本周/本月</summary>
        public PeriodCountDto ManualAdded { get; set; } = new();
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

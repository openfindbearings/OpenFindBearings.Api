using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Queries.Admin.GetDashboardStats
{
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        // 改动说明（v1.50.0）：替代品新增按来源拆分统计，注入替代关系仓储
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IBrandRepository brandRepository,
            IBearingTypeRepository bearingTypeRepository,
            IMerchantDocumentRepository documentRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IBearingInterchangeRepository interchangeRepository,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _correctionRepository = correctionRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _brandRepository = brandRepository;
            _bearingTypeRepository = bearingTypeRepository;
            _documentRepository = documentRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> Handle(
            GetDashboardStatsQuery request,
            CancellationToken cancellationToken)
        {
            // 改动说明（v1.36.1 日界收口）：今日/本周/本月统计从 UTC 零点改为 BusinessClock 业务日界，
            // 与积分/额度口径统一（全仓时间审计后 Api 侧最后一处自算"今天"）；
            // 日历运算（周起点/月起点）在业务日历上做，减偏移折回 UTC 后与 timestamptz 列比较
            var todayStart = BusinessClock.TodayUtc;
            // 改动说明（v2.13.2 周起点 bug 修复）：原内联 AddDays 公式周日越界（得到未来周一），
            //   周/月起点统一收口到 BusinessClock（含 SpecifyKind 防 Npgsql timestamptz 崩溃）
            var weekStart = BusinessClock.BusinessWeekStartUtc;
            var monthStart = BusinessClock.BusinessMonthStartUtc;

            // 改动说明（v1.50.0 来源拆分）：轴承/替代品新增改按来源拆分（互联网 / 人工），
            //   商家新增改按"互联网同步新增 / 入驻新增（OnboardedAt，含认领）"；替代原整块的假数据与单一计数
            var bearingTotal = await _bearingRepository.GetTotalCountAsync(new BearingSearchParams(), cancellationToken);
            var bearingInternetToday = await _bearingRepository.GetCountSinceBySourceAsync(todayStart, true, cancellationToken);
            var bearingInternetWeek = await _bearingRepository.GetCountSinceBySourceAsync(weekStart, true, cancellationToken);
            var bearingInternetMonth = await _bearingRepository.GetCountSinceBySourceAsync(monthStart, true, cancellationToken);
            var bearingManualToday = await _bearingRepository.GetCountSinceBySourceAsync(todayStart, false, cancellationToken);
            var bearingManualWeek = await _bearingRepository.GetCountSinceBySourceAsync(weekStart, false, cancellationToken);
            var bearingManualMonth = await _bearingRepository.GetCountSinceBySourceAsync(monthStart, false, cancellationToken);

            var merchantTotal = await _merchantRepository.GetTotalCountAsync(cancellationToken);
            var merchantVerified = await _merchantRepository.GetVerifiedCountAsync(cancellationToken);
            var merchantOnboardedTotal = await _merchantRepository.GetOnboardedCountAsync(cancellationToken);
            var merchantPendingApplications = await _merchantRepository.GetPendingApplicationCountAsync(cancellationToken);
            var merchantInternetToday = await _merchantRepository.GetInternetAddedCountSinceAsync(todayStart, cancellationToken);
            var merchantInternetWeek = await _merchantRepository.GetInternetAddedCountSinceAsync(weekStart, cancellationToken);
            var merchantInternetMonth = await _merchantRepository.GetInternetAddedCountSinceAsync(monthStart, cancellationToken);
            var merchantOnboardedToday = await _merchantRepository.GetOnboardedCountSinceAsync(todayStart, cancellationToken);
            var merchantOnboardedWeek = await _merchantRepository.GetOnboardedCountSinceAsync(weekStart, cancellationToken);
            var merchantOnboardedMonth = await _merchantRepository.GetOnboardedCountSinceAsync(monthStart, cancellationToken);
            var merchantTypeDist = await _merchantRepository.GetTypeDistributionAsync(cancellationToken);

            var interchangeTotal = await _interchangeRepository.GetTotalCountAsync(cancellationToken);
            // 改动说明（v2.17.0 替代品字典化）：删除替代品待审核计数（替代关系不再有"候选待审"，该值恒为 0）
            var interchangeInternetToday = await _interchangeRepository.GetCountSinceBySourceAsync(todayStart, true, cancellationToken);
            var interchangeInternetWeek = await _interchangeRepository.GetCountSinceBySourceAsync(weekStart, true, cancellationToken);
            var interchangeInternetMonth = await _interchangeRepository.GetCountSinceBySourceAsync(monthStart, true, cancellationToken);
            var interchangeManualToday = await _interchangeRepository.GetCountSinceBySourceAsync(todayStart, false, cancellationToken);
            var interchangeManualWeek = await _interchangeRepository.GetCountSinceBySourceAsync(weekStart, false, cancellationToken);
            var interchangeManualMonth = await _interchangeRepository.GetCountSinceBySourceAsync(monthStart, false, cancellationToken);

            var userTotal = await _userRepository.GetCountSinceAsync(DateTime.MinValue, cancellationToken);
            var userToday = await _userRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var roleDist = await _userRepository.GetRoleDistributionAsync(cancellationToken);

            var correctionPending = await _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Pending, cancellationToken);
            var correctionApproved = await _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Approved, cancellationToken);
            var correctionRejected = await _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Rejected, cancellationToken);
            var correctionToday = await _correctionRepository.GetCountSinceAsync(todayStart, cancellationToken);

            var bearingBrandDist = await _bearingRepository.GetBearingCountByBrandAsync(cancellationToken);
            var bearingTypeDist = await _bearingRepository.GetBearingCountByTypeAsync(cancellationToken);
            var pendingMerchantBearings = await _merchantBearingRepository.GetPendingApprovalCountAsync(cancellationToken);

            var brandTotal = await _brandRepository.GetTotalCountAsync(cancellationToken);
            var typeTotal = await _bearingTypeRepository.GetTotalCountAsync(cancellationToken);
            var pendingDocuments = await _documentRepository.GetPendingCountAsync(cancellationToken);

            var correctionTotal = correctionPending + correctionApproved + correctionRejected;

            var topBrands = new List<BrandDistributionDto>();
            if (bearingBrandDist.Count > 0)
            {
                var allBrands = await _brandRepository.GetAllAsync(cancellationToken);
                var brandMap = allBrands.ToDictionary(b => b.Id, b => b.Name);
                topBrands = bearingBrandDist
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .Select(x => new BrandDistributionDto
                    {
                        BrandName = brandMap.GetValueOrDefault(x.Key, x.Key.ToString()),
                        Count = x.Value
                    })
                    .ToList();
            }

            var topTypes = new List<TypeDistributionDto>();
            if (bearingTypeDist.Count > 0)
            {
                var allTypes = await _bearingTypeRepository.GetAllAsync(cancellationToken);
                var typeMap = allTypes.ToDictionary(t => t.Id, t => t.Name);
                topTypes = bearingTypeDist
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .Select(x => new TypeDistributionDto
                    {
                        TypeName = typeMap.GetValueOrDefault(x.Key, x.Key.ToString()),
                        Count = x.Value
                    })
                    .ToList();
            }

            var typeDistribution = merchantTypeDist
                .Select(x => new MerchantTypeDistributionDto
                {
                    TypeName = x.Key.ToString(),
                    Count = x.Value
                })
                .ToList();

            var adminCount = roleDist.GetValueOrDefault("Admin", 0);
            var individualCount = roleDist.GetValueOrDefault("Individual", 0);

            // 改动说明：商户员工/管理员数量改按成员表统计（商户域角色已从全局角色迁到成员行）
            var merchantMemberCount = await _merchantMemberRepository.CountActiveAsync(cancellationToken);

            return new DashboardStatsDto
            {
                StatsTime = DateTime.UtcNow,
                Bearings = new BearingStatsDto
                {
                    TotalCount = bearingTotal,
                    InternetAdded = new PeriodCountDto { Today = bearingInternetToday, Week = bearingInternetWeek, Month = bearingInternetMonth },
                    ManualAdded = new PeriodCountDto { Today = bearingManualToday, Week = bearingManualWeek, Month = bearingManualMonth },
                    TopBrands = topBrands,
                    TopTypes = topTypes
                },
                Brands = new BrandStatsDto
                {
                    TotalCount = brandTotal
                },
                Types = new TypeStatsDto
                {
                    TotalCount = typeTotal
                },
                Merchants = new MerchantStatsDto
                {
                    TotalCount = merchantTotal,
                    VerifiedCount = merchantVerified,
                    OnboardedCount = merchantOnboardedTotal,
                    PendingApplicationCount = merchantPendingApplications,
                    InternetAdded = new PeriodCountDto { Today = merchantInternetToday, Week = merchantInternetWeek, Month = merchantInternetMonth },
                    Onboarded = new PeriodCountDto { Today = merchantOnboardedToday, Week = merchantOnboardedWeek, Month = merchantOnboardedMonth },
                    TypeDistribution = typeDistribution
                },
                Interchanges = new InterchangesStatsDto
                {
                    TotalCount = interchangeTotal,
                    InternetAdded = new PeriodCountDto { Today = interchangeInternetToday, Week = interchangeInternetWeek, Month = interchangeInternetMonth },
                    ManualAdded = new PeriodCountDto { Today = interchangeManualToday, Week = interchangeManualWeek, Month = interchangeManualMonth }
                },
                Users = new UserStatsDto
                {
                    TotalCount = userTotal,
                    AdminCount = adminCount,
                    MerchantStaffCount = merchantMemberCount,
                    IndividualCount = individualCount,
                    TodayRegistered = userToday,
                    ActiveToday = 0
                },
                Corrections = new CorrectionStatsDto
                {
                    TotalCount = correctionTotal,
                    PendingCount = correctionPending,
                    ApprovedCount = correctionApproved,
                    RejectedCount = correctionRejected,
                    TodaySubmitted = correctionToday
                },
                Pending = new PendingStatsDto
                {
                    PendingMerchantBearings = pendingMerchantBearings,
                    PendingCorrections = correctionPending,
                    PendingDocuments = pendingDocuments
                },
                // 改动说明（v1.50.0 图表下钻）：回传业务日历起点，前端点柱子跳商家列表时按同一日界筛选
                Periods = new StatsPeriodsDto
                {
                    Today = BusinessClock.Today.ToString("yyyy-MM-dd"),
                    WeekStart = (weekStart + BusinessClock.Offset).ToString("yyyy-MM-dd"),
                    MonthStart = (monthStart + BusinessClock.Offset).ToString("yyyy-MM-dd")
                }
            };
        }
    }
}

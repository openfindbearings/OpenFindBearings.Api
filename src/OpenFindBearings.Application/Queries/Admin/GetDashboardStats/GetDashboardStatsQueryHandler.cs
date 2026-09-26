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
            _logger = logger;
        }

        public async Task<DashboardStatsDto> Handle(
            GetDashboardStatsQuery request,
            CancellationToken cancellationToken)
        {
            // 改动说明（v1.36.1 日界收口）：今日/本周/本月统计从 UTC 零点改为 BusinessClock 业务日界，
            // 与积分/额度口径统一（全仓时间审计后 Api 侧最后一处自算"今天"）；
            // 日历运算（周起点/月起点）在业务日历上做，减偏移折回 UTC 后与 timestamptz 列比较
            var bizToday = BusinessClock.Today;
            var todayStart = BusinessClock.TodayUtc;
            var weekStart = bizToday.AddDays(-(int)bizToday.DayOfWeek + (int)DayOfWeek.Monday) - BusinessClock.Offset;
            // 改动说明（rc.32 崩溃修复）：new DateTime(y,m,d) 的 Kind=Unspecified，减 TimeSpan 也不改变 Kind，
            // 直接进 Npgsql timestamptz 参数即抛 ArgumentException（dashboard 全 N/A 的根因）；
            // 必须 SpecifyKind 显式标 UTC。weekStart 无此雷——bizToday 源自 UtcNow，AddDays/减法保留 Kind=Utc
            var monthStart = DateTime.SpecifyKind(new DateTime(bizToday.Year, bizToday.Month, 1), DateTimeKind.Utc) - BusinessClock.Offset;

            var bearingTotal = await _bearingRepository.GetTotalCountAsync(new BearingSearchParams(), cancellationToken);
            var bearingToday = await _bearingRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var bearingWeek = await _bearingRepository.GetCountSinceAsync(weekStart, cancellationToken);
            var bearingMonth = await _bearingRepository.GetCountSinceAsync(monthStart, cancellationToken);

            var merchantTotal = await _merchantRepository.GetTotalCountAsync(cancellationToken);
            var merchantVerified = await _merchantRepository.GetVerifiedCountAsync(cancellationToken);
            var merchantPendingApplications = await _merchantRepository.GetPendingApplicationCountAsync(cancellationToken);
            var merchantToday = await _merchantRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var merchantTypeDist = await _merchantRepository.GetTypeDistributionAsync(cancellationToken);

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
                    TodayAdded = bearingToday,
                    ThisWeekAdded = bearingWeek,
                    ThisMonthAdded = bearingMonth,
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
                    PendingApplicationCount = merchantPendingApplications,
                    TodayRegistered = merchantToday,
                    TypeDistribution = typeDistribution
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
                }
            };
        }
    }
}

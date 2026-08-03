using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;
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
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IBrandRepository brandRepository,
            IBearingTypeRepository bearingTypeRepository,
            ILicenseVerificationRepository licenseRepository,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _correctionRepository = correctionRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _brandRepository = brandRepository;
            _bearingTypeRepository = bearingTypeRepository;
            _licenseRepository = licenseRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> Handle(
            GetDashboardStatsQuery request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var bearingTotalTask = _bearingRepository.GetTotalCountAsync(new BearingSearchParams(), cancellationToken);
            var bearingTodayTask = _bearingRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var bearingWeekTask = _bearingRepository.GetCountSinceAsync(weekStart, cancellationToken);
            var bearingMonthTask = _bearingRepository.GetCountSinceAsync(monthStart, cancellationToken);

            var merchantTotalTask = _merchantRepository.GetTotalCountAsync(cancellationToken);
            var merchantVerifiedTask = _merchantRepository.GetVerifiedCountAsync(cancellationToken);
            var merchantTodayTask = _merchantRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var merchantTypeDistTask = _merchantRepository.GetTypeDistributionAsync(cancellationToken);

            var userTotalTask = _userRepository.GetCountSinceAsync(DateTime.MinValue, cancellationToken);
            var userTodayTask = _userRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var roleDistTask = _userRepository.GetRoleDistributionAsync(cancellationToken);

            var correctionPendingTask = _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Pending, cancellationToken);
            var correctionApprovedTask = _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Approved, cancellationToken);
            var correctionRejectedTask = _correctionRepository.GetCountByStatusAsync(Domain.Enums.CorrectionStatus.Rejected, cancellationToken);
            var correctionTodayTask = _correctionRepository.GetCountSinceAsync(todayStart, cancellationToken);

            var bearingBrandDistTask = _bearingRepository.GetBearingCountByBrandAsync(cancellationToken);
            var bearingTypeDistTask = _bearingRepository.GetBearingCountByTypeAsync(cancellationToken);
            var pendingMerchantBearingsTask = _merchantBearingRepository.GetPendingApprovalCountAsync(cancellationToken);

            var brandTotalTask = _brandRepository.GetTotalCountAsync(cancellationToken);
            var typeTotalTask = _bearingTypeRepository.GetTotalCountAsync(cancellationToken);
            var pendingLicenseTask = _licenseRepository.GetPendingCountAsync(cancellationToken);
            var pendingMerchantVerifyTask = _merchantRepository.GetVerifiedCountAsync(cancellationToken);

            await Task.WhenAll(
                bearingTotalTask, bearingTodayTask, bearingWeekTask, bearingMonthTask,
                merchantTotalTask, merchantVerifiedTask, merchantTodayTask, merchantTypeDistTask,
                userTotalTask, userTodayTask, roleDistTask,
                correctionPendingTask, correctionApprovedTask, correctionRejectedTask, correctionTodayTask,
                bearingBrandDistTask, bearingTypeDistTask, pendingMerchantBearingsTask,
                brandTotalTask, typeTotalTask, pendingLicenseTask, pendingMerchantVerifyTask);

            var bearingTotal = bearingTotalTask.Result;
            var bearingToday = bearingTodayTask.Result;
            var bearingWeek = bearingWeekTask.Result;
            var bearingMonth = bearingMonthTask.Result;

            var merchantTotal = merchantTotalTask.Result;
            var merchantVerified = merchantVerifiedTask.Result;
            var merchantToday = merchantTodayTask.Result;
            var merchantTypeDist = merchantTypeDistTask.Result;

            var userTotal = userTotalTask.Result;
            var userToday = userTodayTask.Result;
            var roleDist = roleDistTask.Result;

            var correctionTotal = correctionPendingTask.Result + correctionApprovedTask.Result + correctionRejectedTask.Result;
            var correctionPending = correctionPendingTask.Result;
            var correctionApproved = correctionApprovedTask.Result;
            var correctionRejected = correctionRejectedTask.Result;
            var correctionToday = correctionTodayTask.Result;

            var bearingBrandDist = bearingBrandDistTask.Result;
            var bearingTypeDist = bearingTypeDistTask.Result;
            var pendingMerchantBearings = pendingMerchantBearingsTask.Result;
            var brandTotal = brandTotalTask.Result;
            var typeTotal = typeTotalTask.Result;
            var pendingLicenses = pendingLicenseTask.Result;
            var pendingMerchantVerifyTotal = pendingMerchantVerifyTask.Result;

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
            var staffCount = roleDist.GetValueOrDefault("MerchantStaff", 0);
            var individualCount = roleDist.GetValueOrDefault("Individual", 0);

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
                    PendingVerification = merchantTotal - merchantVerified,
                    TodayRegistered = merchantToday,
                    TypeDistribution = typeDistribution
                },
                Users = new UserStatsDto
                {
                    TotalCount = userTotal,
                    AdminCount = adminCount,
                    MerchantStaffCount = staffCount,
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
                    PendingLicenses = pendingLicenses,
                    PendingMerchantVerifications = merchantTotal - pendingMerchantVerifyTotal
                }
            };
        }
    }
}

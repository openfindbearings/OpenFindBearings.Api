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

            var bearingTotal = await _bearingRepository.GetTotalCountAsync(new BearingSearchParams(), cancellationToken);
            var bearingToday = await _bearingRepository.GetCountSinceAsync(todayStart, cancellationToken);
            var bearingWeek = await _bearingRepository.GetCountSinceAsync(weekStart, cancellationToken);
            var bearingMonth = await _bearingRepository.GetCountSinceAsync(monthStart, cancellationToken);

            var merchantTotal = await _merchantRepository.GetTotalCountAsync(cancellationToken);
            var merchantVerified = await _merchantRepository.GetVerifiedCountAsync(cancellationToken);
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
            var pendingLicenses = await _licenseRepository.GetPendingCountAsync(cancellationToken);
            var pendingMerchantVerifyTotal = await _merchantRepository.GetVerifiedCountAsync(cancellationToken);

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

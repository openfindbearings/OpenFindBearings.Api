using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Queries.Admin.GetDashboardStats
{
    /// <summary>
    /// 获取仪表盘统计数据查询处理器
    /// </summary>
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetDashboardStatsQueryHandler> _logger;

        public GetDashboardStatsQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetDashboardStatsQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _correctionRepository = correctionRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> Handle(
            GetDashboardStatsQuery request,
            CancellationToken cancellationToken)
        {
            var bearingCount = await _bearingRepository.GetTotalCountAsync(new BearingSearchParams(), cancellationToken);
            var merchantCount = await _merchantRepository.GetTotalCountAsync(cancellationToken);

            return new DashboardStatsDto
            {
                StatsTime = DateTime.UtcNow,
                Bearings = new BearingStatsDto
                {
                    TotalCount = bearingCount,
                    TodayAdded = 0,
                    ThisWeekAdded = 0,
                    ThisMonthAdded = 0,
                    TopBrands = new List<BrandDistributionDto>(),
                    TopTypes = new List<TypeDistributionDto>()
                },
                Merchants = new MerchantStatsDto
                {
                    TotalCount = merchantCount,
                    VerifiedCount = 0,
                    PendingVerification = 0,
                    TodayRegistered = 0,
                    TypeDistribution = new List<MerchantTypeDistributionDto>()
                },
                Users = new UserStatsDto
                {
                    TotalCount = 0,
                    AdminCount = 0,
                    MerchantStaffCount = 0,
                    IndividualCount = 0,
                    TodayRegistered = 0,
                    ActiveToday = 0
                },
                Corrections = new CorrectionStatsDto
                {
                    TotalCount = 0,
                    PendingCount = 0,
                    ApprovedCount = 0,
                    RejectedCount = 0,
                    TodaySubmitted = 0
                },
                Pending = new PendingStatsDto
                {
                    PendingMerchantBearings = 0,
                    PendingCorrections = 0,
                    PendingMerchantVerifications = 0
                }
            };
        }
    }
}

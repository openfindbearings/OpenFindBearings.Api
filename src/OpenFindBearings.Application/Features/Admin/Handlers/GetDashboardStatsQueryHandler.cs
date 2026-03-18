using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Application.Features.Admin.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Admin.Handlers
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
            // 这里需要根据实际仓储方法实现统计
            // 以下为示例代码，需要根据实际仓储API调整

            return new DashboardStatsDto
            {
                StatsTime = DateTime.UtcNow,
                Bearings = new BearingStatsDto
                {
                    TotalCount = 1000, // await _bearingRepository.CountAsync(cancellationToken),
                    TodayAdded = 10,
                    ThisWeekAdded = 50,
                    ThisMonthAdded = 200,
                    TopBrands = new List<BrandDistributionDto>(),
                    TopTypes = new List<TypeDistributionDto>()
                },
                Merchants = new MerchantStatsDto
                {
                    TotalCount = 100,
                    VerifiedCount = 80,
                    PendingVerification = 10,
                    TodayRegistered = 2,
                    TypeDistribution = new List<MerchantTypeDistributionDto>()
                },
                Users = new UserStatsDto
                {
                    TotalCount = 500,
                    AdminCount = 5,
                    MerchantStaffCount = 150,
                    IndividualCount = 345,
                    TodayRegistered = 8,
                    ActiveToday = 45
                },
                Corrections = new CorrectionStatsDto
                {
                    TotalCount = 50,
                    PendingCount = 12,
                    ApprovedCount = 30,
                    RejectedCount = 8,
                    TodaySubmitted = 3
                },
                Pending = new PendingStatsDto
                {
                    PendingMerchantBearings = 15,
                    PendingCorrections = 12,
                    PendingMerchantVerifications = 5
                }
            };
        }
    }
}

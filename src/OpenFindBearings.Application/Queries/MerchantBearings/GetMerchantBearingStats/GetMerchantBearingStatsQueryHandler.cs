using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingStats
{
    /// <summary>
    /// 获取商家-轴承关联统计信息查询处理器
    /// </summary>
    public class GetMerchantBearingStatsQueryHandler : IRequestHandler<GetMerchantBearingStatsQuery, MerchantBearingStatsDto>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingStatsQueryHandler> _logger;

        public GetMerchantBearingStatsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingStatsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<MerchantBearingStatsDto> Handle(
            GetMerchantBearingStatsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家-轴承关联统计信息: MerchantId={MerchantId}, BearingId={BearingId}",
                request.MerchantId, request.BearingId);

            IEnumerable<MerchantBearing> merchantBearings;

            if (request.MerchantId.HasValue)
            {
                merchantBearings = await _merchantBearingRepository.GetByMerchantAsync(request.MerchantId.Value, cancellationToken);
            }
            else if (request.BearingId.HasValue)
            {
                merchantBearings = await _merchantBearingRepository.GetByBearingAsync(request.BearingId.Value, cancellationToken);
            }
            else
            {
                // 如果没有指定条件，返回空统计
                return new MerchantBearingStatsDto();
            }

            var list = merchantBearings.ToList();

            return new MerchantBearingStatsDto
            {
                TotalCount = list.Count,
                // v2.15.0 状态统一：出售中/补货中 + 审核中（原 FeaturedCount 僵尸统计已删）
                OnSaleCount = list.Count(mb => mb.SaleState == SaleState.OnSale),
                PendingCount = list.Count(mb => mb.ReviewStatus == ListingReviewStatus.Pending),
                MerchantCount = request.MerchantId.HasValue ? 1 : list.Select(mb => mb.MerchantId).Distinct().Count(),
                BearingCount = request.BearingId.HasValue ? 1 : list.Select(mb => mb.BearingId).Distinct().Count()
            };
        }
    }
}

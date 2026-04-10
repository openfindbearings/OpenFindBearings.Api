using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByBearing
{
    /// <summary>
    /// 获取指定轴承的在售商家列表查询处理器
    /// </summary>
    public class GetMerchantBearingsByBearingQueryHandler : IRequestHandler<GetMerchantBearingsByBearingQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingsByBearingQueryHandler> _logger;

        public GetMerchantBearingsByBearingQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingsByBearingQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMerchantBearingsByBearingQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取轴承的在售商家列表: BearingId={BearingId}, Page={Page}, PageSize={PageSize}",
                request.BearingId, request.Page, request.PageSize);

            var merchantBearings = await _merchantBearingRepository.GetByBearingAsync(request.BearingId, cancellationToken);

            // 只返回在售的商品
            merchantBearings = merchantBearings.Where(mb => mb.IsOnSale);

            var totalCount = merchantBearings.Count();
            var items = merchantBearings
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mb => mb.ToDto(request.IsAuthenticated))
                .ToList();

            return new PagedResult<MerchantBearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

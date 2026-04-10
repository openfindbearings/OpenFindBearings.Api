using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.History.GetMyBearingHistory
{
    /// <summary>
    /// 获取我的轴承浏览历史查询处理器
    /// </summary>
    public class GetMyBearingHistoryQueryHandler : IRequestHandler<GetMyBearingHistoryQuery, PagedResult<BearingHistoryDto>>
    {
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly ILogger<GetMyBearingHistoryQueryHandler> _logger;

        public GetMyBearingHistoryQueryHandler(
            IUserBearingHistoryRepository historyRepository,
            ILogger<GetMyBearingHistoryQueryHandler> logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public async Task<PagedResult<BearingHistoryDto>> Handle(
            GetMyBearingHistoryQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户轴承浏览历史: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.Page, request.PageSize);

            var histories = await _historyRepository.GetByUserIdAsync(
                request.UserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalCount = await _historyRepository.CountByUserIdAsync(request.UserId, cancellationToken);

            var items = histories
                .Where(h => h.Bearing != null)
                .Select(h => h.ToDto())
                .ToList();

            return new PagedResult<BearingHistoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

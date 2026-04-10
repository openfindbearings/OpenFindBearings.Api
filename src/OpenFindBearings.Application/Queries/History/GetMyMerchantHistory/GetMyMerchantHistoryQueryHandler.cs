using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.History.GetMyMerchantHistory
{
    /// <summary>
    /// 获取我的商家浏览历史查询处理器
    /// </summary>
    public class GetMyMerchantHistoryQueryHandler : IRequestHandler<GetMyMerchantHistoryQuery, PagedResult<MerchantHistoryDto>>
    {
        private readonly IUserMerchantHistoryRepository _historyRepository;
        private readonly ILogger<GetMyMerchantHistoryQueryHandler> _logger;

        public GetMyMerchantHistoryQueryHandler(
            IUserMerchantHistoryRepository historyRepository,
            ILogger<GetMyMerchantHistoryQueryHandler> logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantHistoryDto>> Handle(
            GetMyMerchantHistoryQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户商家浏览历史: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.Page, request.PageSize);

            var histories = await _historyRepository.GetByUserIdAsync(
                request.UserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalCount = histories.Count;

            var items = histories
                .Where(h => h.Merchant != null)
                .Select(h => h.ToDto())
                .ToList();

            return new PagedResult<MerchantHistoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

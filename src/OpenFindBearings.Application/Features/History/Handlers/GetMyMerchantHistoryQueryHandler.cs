using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.DTOs;
using OpenFindBearings.Application.Features.History.Queries;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 获取我的商家浏览历史查询处理器
    /// </summary>
    public class GetMyMerchantHistoryQueryHandler : IRequestHandler<GetMyMerchantHistoryQuery, PagedResult<MerchantHistoryDto>>
    {
        private readonly IUserMerchantHistoryRepository _historyRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyMerchantHistoryQueryHandler> _logger;

        public GetMyMerchantHistoryQueryHandler(
            IUserMerchantHistoryRepository historyRepository,
            IUserRepository userRepository,
            ILogger<GetMyMerchantHistoryQueryHandler> logger)
        {
            _historyRepository = historyRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantHistoryDto>> Handle(GetMyMerchantHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return new PagedResult<MerchantHistoryDto>();
            }

            var histories = await _historyRepository.GetByUserIdAsync(user.Id, request.Page, request.PageSize, cancellationToken);

            var totalCount = histories.Count;

            var items = histories.Select(h => new MerchantHistoryDto
            {
                Id = h.Id,
                MerchantId = h.MerchantId,
                MerchantName = h.Merchant?.Name ?? string.Empty,
                CompanyName = h.Merchant?.CompanyName,
                ViewedAt = h.ViewedAt,
                ViewCount = 1
            }).ToList();

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

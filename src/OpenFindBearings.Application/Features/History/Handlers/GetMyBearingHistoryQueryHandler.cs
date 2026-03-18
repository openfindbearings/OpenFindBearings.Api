using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.DTOs;
using OpenFindBearings.Application.Features.History.Queries;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 获取我的轴承浏览历史查询处理器
    /// </summary>
    public class GetMyBearingHistoryQueryHandler : IRequestHandler<GetMyBearingHistoryQuery, PagedResult<BearingHistoryDto>>
    {
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyBearingHistoryQueryHandler> _logger;

        public GetMyBearingHistoryQueryHandler(
            IUserBearingHistoryRepository historyRepository,
            IUserRepository userRepository,
            ILogger<GetMyBearingHistoryQueryHandler> logger)
        {
            _historyRepository = historyRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<BearingHistoryDto>> Handle(GetMyBearingHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return new PagedResult<BearingHistoryDto>();
            }

            var histories = await _historyRepository.GetByUserIdAsync(user.Id, request.Page, request.PageSize, cancellationToken);

            // 获取总记录数
            var totalCount = await _historyRepository.CountByUserIdAsync(user.Id, cancellationToken);

            var items = new List<BearingHistoryDto>();
            foreach (var history in histories)
            {
                if (history.Bearing == null) continue;

                // 获取该轴承的总浏览次数（从 Bearing 实体获取）
                var viewCount = history.Bearing.ViewCount;

                items.Add(new BearingHistoryDto
                {
                    Id = history.Id,
                    BearingId = history.BearingId,
                    BearingPartNumber = history.Bearing.PartNumber,
                    BearingName = history.Bearing.Name,
                    BrandName = history.Bearing.Brand?.Name,
                    ViewedAt = history.ViewedAt,
                    ViewCount = viewCount
                });
            }

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

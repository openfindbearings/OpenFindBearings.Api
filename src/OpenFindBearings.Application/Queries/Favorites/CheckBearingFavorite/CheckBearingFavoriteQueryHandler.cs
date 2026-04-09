using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Favorites.CheckBearingFavorite
{
    /// <summary>
    /// 检查轴承收藏状态查询处理器
    /// </summary>
    public class CheckBearingFavoriteQueryHandler : IRequestHandler<CheckBearingFavoriteQuery, bool>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly ILogger<CheckBearingFavoriteQueryHandler> _logger;

        public CheckBearingFavoriteQueryHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            ILogger<CheckBearingFavoriteQueryHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckBearingFavoriteQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("检查轴承收藏状态: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            return await _favoriteRepository.ExistsAsync(request.UserId, request.BearingId, cancellationToken);
        }
    }
}

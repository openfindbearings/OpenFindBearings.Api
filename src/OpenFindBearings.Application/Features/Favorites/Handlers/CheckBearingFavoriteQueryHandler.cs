using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Favorites.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Favorites.Handlers
{
    /// <summary>
    /// 检查轴承收藏状态查询处理器
    /// </summary>
    public class CheckBearingFavoriteQueryHandler : IRequestHandler<CheckBearingFavoriteQuery, bool>
    {
        private readonly IUserFavoriteRepository _favoriteRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CheckBearingFavoriteQueryHandler> _logger;

        public CheckBearingFavoriteQueryHandler(
            IUserFavoriteRepository favoriteRepository,
            IUserRepository userRepository,
            ILogger<CheckBearingFavoriteQueryHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckBearingFavoriteQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            return await _favoriteRepository.ExistsAsync(user.Id, request.BearingId, cancellationToken);
        }
    }
}

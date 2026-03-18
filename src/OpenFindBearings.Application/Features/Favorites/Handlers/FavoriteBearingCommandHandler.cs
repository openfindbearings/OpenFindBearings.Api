using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Favorites.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Favorites.Handlers
{
    /// <summary>
    /// 收藏轴承命令处理器
    /// </summary>
    public class FavoriteBearingCommandHandler : IRequestHandler<FavoriteBearingCommand, bool>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<FavoriteBearingCommandHandler> _logger;

        public FavoriteBearingCommandHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IMediator mediator,
            ILogger<FavoriteBearingCommandHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(FavoriteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户收藏轴承: UserId={AuthUserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 获取业务用户ID
            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            // 检查轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.BearingId}");
            }

            // 检查是否已收藏
            var exists = await _favoriteRepository.ExistsAsync(user.Id, request.BearingId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("用户已收藏该轴承: UserId={UserId}, BearingId={BearingId}",
                    user.Id, request.BearingId);
                return false;
            }

            // 执行收藏
            user.FavoriteBearing(request.BearingId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // 发布领域事件
            await _mediator.Publish(new BearingFavoritedEvent(user.Id, request.BearingId), cancellationToken);

            _logger.LogInformation("用户收藏轴承成功: UserId={UserId}, BearingId={BearingId}",
                user.Id, request.BearingId);

            return true;
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Favorites.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Favorites.Handlers
{
    /// <summary>
    /// 取消收藏轴承命令处理器
    /// </summary>
    public class UnfavoriteBearingCommandHandler : IRequestHandler<UnfavoriteBearingCommand>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<UnfavoriteBearingCommandHandler> _logger;

        public UnfavoriteBearingCommandHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            IUserRepository userRepository,
            IMediator mediator,
            ILogger<UnfavoriteBearingCommandHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(UnfavoriteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户取消收藏轴承: UserId={AuthUserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            var exists = await _favoriteRepository.ExistsAsync(user.Id, request.BearingId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("用户未收藏该轴承: UserId={UserId}, BearingId={BearingId}",
                    user.Id, request.BearingId);
                return;
            }

            user.UnfavoriteBearing(request.BearingId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _mediator.Publish(new BearingUnfavoritedEvent(user.Id, request.BearingId), cancellationToken);

            _logger.LogInformation("用户取消收藏轴承成功: UserId={UserId}, BearingId={BearingId}",
                user.Id, request.BearingId);
        }
    }
}

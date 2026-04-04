using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Favorites.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

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
            _logger.LogInformation("用户取消收藏轴承: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 检查是否存在收藏
            var exists = await _favoriteRepository.ExistsAsync(request.UserId, request.BearingId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("用户未收藏该轴承: UserId={UserId}, BearingId={BearingId}",
                    request.UserId, request.BearingId);
                return;
            }

            // 获取用户实体
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            user.UnfavoriteBearing(request.BearingId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _mediator.Publish(new BearingUnfavoritedEvent(request.UserId, request.BearingId), cancellationToken);

            _logger.LogInformation("用户取消收藏轴承成功: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);
        }
    }
}

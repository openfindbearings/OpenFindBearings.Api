using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Follows.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Follows.Handlers
{
    /// <summary>
    /// 取消关注商家命令处理器
    /// </summary>
    public class UnfollowMerchantCommandHandler : IRequestHandler<UnfollowMerchantCommand>
    {
        private readonly IUserFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<UnfollowMerchantCommandHandler> _logger;

        public UnfollowMerchantCommandHandler(
            IUserFollowRepository followRepository,
            IUserRepository userRepository,
            IMediator mediator,
            ILogger<UnfollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(UnfollowMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户取消关注商家: UserId={AuthUserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            var exists = await _followRepository.ExistsAsync(user.Id, request.MerchantId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("用户未关注该商家: UserId={UserId}, MerchantId={MerchantId}",
                    user.Id, request.MerchantId);
                return;
            }

            user.UnfollowMerchant(request.MerchantId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _mediator.Publish(new MerchantUnfollowedEvent(user.Id, request.MerchantId), cancellationToken);

            _logger.LogInformation("用户取消关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);
        }
    }
}

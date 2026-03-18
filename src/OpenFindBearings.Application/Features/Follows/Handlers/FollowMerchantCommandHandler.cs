using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Follows.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Follows.Handlers
{
    /// <summary>
    /// 关注商家命令处理器
    /// </summary>
    public class FollowMerchantCommandHandler : IRequestHandler<FollowMerchantCommand, bool>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<FollowMerchantCommandHandler> _logger;

        public FollowMerchantCommandHandler(
            IUserMerchantFollowRepository followRepository,
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IMediator mediator,
            ILogger<FollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(FollowMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户关注商家: UserId={AuthUserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            var exists = await _followRepository.ExistsAsync(user.Id, request.MerchantId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("用户已关注该商家: UserId={UserId}, MerchantId={MerchantId}",
                    user.Id, request.MerchantId);
                return false;
            }

            user.FollowMerchant(request.MerchantId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _mediator.Publish(new MerchantFollowedEvent(user.Id, request.MerchantId), cancellationToken);

            _logger.LogInformation("用户关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);

            return true;
        }
    }
}

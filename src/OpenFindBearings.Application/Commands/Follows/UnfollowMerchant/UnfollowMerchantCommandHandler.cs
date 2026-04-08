using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Follows.UnfollowMerchant
{
    /// <summary>
    /// 取消关注商家命令处理器
    /// </summary>
    public class UnfollowMerchantCommandHandler : IRequestHandler<UnfollowMerchantCommand>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UnfollowMerchantCommandHandler> _logger;

        public UnfollowMerchantCommandHandler(
            IUserMerchantFollowRepository followRepository,
            IUserRepository userRepository,
            ILogger<UnfollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(UnfollowMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户取消关注商家: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            // 检查是否存在关注
            var exists = await _followRepository.ExistsAsync(request.UserId, request.MerchantId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("用户未关注该商家: UserId={UserId}, MerchantId={MerchantId}",
                    request.UserId, request.MerchantId);
                return;
            }

            // 获取用户实体
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            user.UnfollowMerchant(request.MerchantId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("用户取消关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);
        }
    }
}

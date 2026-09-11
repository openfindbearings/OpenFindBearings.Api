using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Follows.FollowMerchant
{
    /// <summary>
    /// 关注商家命令处理器
    /// </summary>
    public class FollowMerchantCommandHandler : IRequestHandler<FollowMerchantCommand, bool>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<FollowMerchantCommandHandler> _logger;

        public FollowMerchantCommandHandler(
            IUserMerchantFollowRepository followRepository,
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            ILogger<FollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(FollowMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户关注商家: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            // 检查商家是否存在
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 检查是否已关注
            var exists = await _followRepository.ExistsAsync(request.UserId, request.MerchantId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("用户已关注该商家: UserId={UserId}, MerchantId={MerchantId}",
                    request.UserId, request.MerchantId);
                return false;
            }

            // 获取用户实体
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            // 执行关注。
            // 改动说明：同收藏修复——原先依赖导航集合 Add + UpdateAsync(user) 令 EF 自动 INSERT
            // 连接表实体，但 BaseEntity 预赋 Guid.Id + Npgsql 无值生成器导致 EF 导航修复判为 Modified，
            // UPDATE 不存在行→整批回滚。现显式 Add 关注实体，关注不修改 Users 行故移除 UpdateAsync
            user.FollowMerchant(request.MerchantId);
            var follow = user.FollowedMerchants.Last(f => f.MerchantId == request.MerchantId);
            await _followRepository.AddAsync(follow, cancellationToken);

            _logger.LogInformation("用户关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            return true;
        }
    }
}

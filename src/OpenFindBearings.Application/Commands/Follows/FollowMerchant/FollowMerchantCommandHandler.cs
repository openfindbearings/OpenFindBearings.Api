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
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ILogger<FollowMerchantCommandHandler> _logger;

        public FollowMerchantCommandHandler(
            IUserMerchantFollowRepository followRepository,
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            ILogger<FollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
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

            // 改动说明（禁止自关注）：自家店铺的关注无业务意义——店主本就能在管理端看到全部经营数据，
            //   自关注会污染对外展示的粉丝数与"我关注的商家"列表，也会产生自己给自己的动态通知。
            //   服务端兜底校验，避免绕过前端直接调用接口。
            var membership = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (membership != null)
            {
                _logger.LogWarning("拒绝自关注: UserId={UserId} 是商户 {MerchantId} 的在职成员",
                    request.UserId, request.MerchantId);
                throw new InvalidOperationException("不能关注自己的商户");
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

            // 改动说明（粉丝数恒 0 修复）：关注行与商户粉丝计数在同一 UnitOfWork 内落库。
            //   历史实现只写连接表、从不更新 Merchants.FollowerCount，商家详情页粉丝数永远显示 0。
            merchant.AdjustFollowerCount(1);
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            _logger.LogInformation("用户关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            return true;
        }
    }
}
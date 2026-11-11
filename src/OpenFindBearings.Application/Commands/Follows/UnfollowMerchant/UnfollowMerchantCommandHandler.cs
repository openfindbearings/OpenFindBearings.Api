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
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<UnfollowMerchantCommandHandler> _logger;

        public UnfollowMerchantCommandHandler(
            IUserMerchantFollowRepository followRepository,
            IMerchantRepository merchantRepository,
            ILogger<UnfollowMerchantCommandHandler> logger)
        {
            _followRepository = followRepository;
            _merchantRepository = merchantRepository;
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

            // 直接删除关注连接表行。
            // 改动说明：同取消收藏——原先 user.UnfollowMerchant() + UpdateAsync(user) 只从
            // 未加载的内存集合移除，对库零效果。显式走仓储 DeleteAsync 按主键删除
            await _followRepository.DeleteAsync(request.UserId, request.MerchantId, cancellationToken);

            // 改动说明（粉丝数恒 0 修复）：与删除关注行同一 UnitOfWork 内递减商家粉丝计数，
            //   与关注处理器对称，保证对外展示的粉丝数与连接表一致。
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant != null)
            {
                merchant.AdjustFollowerCount(-1);
                await _merchantRepository.UpdateAsync(merchant, cancellationToken);
            }

            _logger.LogInformation("用户取消关注商家成功: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);
        }
    }
}
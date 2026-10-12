using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.RemoveMerchantBearing
{
    /// <summary>
    /// 删除商家商品命令处理器（v2.15.0 商家删除）。
    /// 成员归属校验（全体成员可删）+ 状态守卫（出售中/补货中禁删，须先下架）+ 物理删除。
    /// 非公域态（仓库中/审核中/已驳回/已下架）一律硬删，不留软删残留。
    /// </summary>
    public class RemoveMerchantBearingCommandHandler : IRequestHandler<RemoveMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<RemoveMerchantBearingCommandHandler> _logger;

        public RemoveMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<RemoveMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        /// <summary>执行删除：归属校验 → 状态守卫 → 物理删除</summary>
        public async Task Handle(RemoveMerchantBearingCommand request, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            // 归属校验：当前用户须为该商户在职成员（全体成员均可删，不区分角色）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权删除其他商家的商品");
            }

            // 状态守卫：出售中/补货中不可删（公域可见，须先下架）
            merchantBearing.EnsureRemovable();

            await _merchantBearingRepository.DeleteAsync(merchantBearing.Id, cancellationToken);
            _logger.LogInformation("商家商品删除成功（物理删除）: Id={Id}", merchantBearing.Id);
        }
    }
}
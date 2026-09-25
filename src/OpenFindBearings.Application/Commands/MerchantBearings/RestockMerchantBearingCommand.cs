using MediatR;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.Restock
{
    /// <summary>
    /// 商品置为补货中命令（v1.36.0 三态销售状态）：
    /// 商家有该型号但暂时缺货——下架但保留"补货中"信号，买家侧商家列表仍展示带徽标
    /// </summary>
    public record RestockMerchantBearingCommand : IRequest
    {
        /// <summary>商家-轴承关联ID</summary>
        public Guid MerchantBearingId { get; init; }

        /// <summary>操作用户ID（须为该商户在职成员）</summary>
        public Guid UserId { get; init; }

        /// <summary>预计到货时间（自由文本，可空，如"一周内"）</summary>
        public string? RestockEta { get; init; }
    }

    /// <summary>
    /// 补货中命令处理器：所有权校验与上/下架一致（成员表判定），
    /// 与既有 PutOnShelf/TakeOffShelf 端点共同构成三态切换
    /// </summary>
    public class RestockMerchantBearingCommandHandler : IRequestHandler<RestockMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;

        public RestockMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(RestockMerchantBearingCommand request, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            // 所有权验证：当前用户必须是该商户的在职成员（与上/下架同口径）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            merchantBearing.MarkAsRestocking(request.RestockEta);
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);
        }
    }
}

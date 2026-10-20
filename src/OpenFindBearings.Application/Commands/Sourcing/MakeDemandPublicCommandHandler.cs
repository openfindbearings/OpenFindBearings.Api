using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 转为公开命令（v1.8.0 公开/邀请）：邀请单进行中时由发布人放开为公开
    /// （需求进公开大厅，人人可见可应答）。解决"邀请后无人应答只能取消重建"的损耗
    /// （原定向单只能取消再发，发布额度与积分不退还）。
    /// 改动说明（v1.8.2 转公开补发通知）：放开后按"匹配集合 − 原邀请收件人"差集补发通知——
    ///   新纳入的认证在售商家也被告知（口径与发布即公开一致），而已被告知过的原收件人不再打扰（绝不二次）
    /// </summary>
    /// <param name="DemandId">寻货单 ID</param>
    /// <param name="UserId">操作人（仅发布人本人可操作，与列表管理按钮口径一致）</param>
    public record MakeDemandPublicCommand(Guid DemandId, Guid UserId) : IRequest;

    /// <summary>
    /// 转为公开处理器（守卫在聚合内：已是公开/已非进行中一律拒绝）
    /// </summary>
    public class MakeDemandPublicCommandHandler : IRequestHandler<MakeDemandPublicCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        // 改动说明（v1.8.2 转公开补发通知）：复用 SourcingMatcher 现算匹配集合 + 事件携带收件人集合，只推新商家
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        // 改动说明（v1.57.3 自交易封堵）：转公开补发时同样排除发布人在职的全部商户，避免自家店铺收到通知
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IPublisher _publisher;

        /// <summary>
        /// 构造：需求仓储 + 匹配所需仓储（轴承/商家商品/商户/成员）+ 事件发布器
        /// </summary>
        public MakeDemandPublicCommandHandler(
            ISourcingDemandRepository demandRepository,
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IPublisher publisher)
        {
            _demandRepository = demandRepository;
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _publisher = publisher;
        }

        /// <inheritdoc/>
        public async Task Handle(MakeDemandPublicCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 权限：仅发布人本人（商户名义单同样限经办人——与"商户寻货列表仅本人出管理按钮"口径一致）
            if (demand.PublisherUserId != request.UserId)
                throw new UnauthorizedAccessException("只有发布人可以将寻货转为公开");

            // 改动说明（v1.8.2）：MakePublic 会清空收件人快照，故先留存原收件人集合，供差集补发判定
            var invitedBefore = demand.InvitedMerchantIds.ToList();

            demand.MakePublic();
            await _demandRepository.UpdateAsync(demand, cancellationToken);

            // 差集补发：只通知"本次放开后新纳入、且此前未被告知"的认证在售商家（原收件人已收到邀请通知，不二次打扰）
            // 改动说明（v1.57.3）：排除集合=发布人在职的全部商户（含商户名义发布的那一家），自家店铺不补发
            var myMerchantIds = (await _memberRepository.GetActiveByUserIdAsync(
                    demand.PublisherUserId, cancellationToken))
                .Select(m => m.MerchantId)
                .ToHashSet();
            if (demand.PublisherMerchantId.HasValue)
                myMerchantIds.Add(demand.PublisherMerchantId.Value);
            var matched = await SourcingMatcher.GetMatchedMerchantIdsAsync(
                _bearingRepository, _merchantBearingRepository, _merchantRepository,
                demand.BearingId, demand.PartNumber, cancellationToken, myMerchantIds);
            var alreadyNotified = invitedBefore.ToHashSet();
            var fresh = matched.Where(id => !alreadyNotified.Contains(id)).ToList();
            if (fresh.Count > 0)
            {
                await _publisher.Publish(new SourcingDemandPublishedEvent(
                    demand.Id, demand.PublisherUserId, demand.BearingId, demand.PartNumber, demand.Brand,
                    SourcingVisibility.Public, fresh), cancellationToken);
            }
        }
    }
}

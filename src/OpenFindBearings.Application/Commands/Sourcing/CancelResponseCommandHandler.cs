using MediatR;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 撤销应答命令（v1.5.0 取消应答）：商户撤回自己对某需求提交的应答，需求回到未应答状态（比价列表移除该商户）。
    /// 撤销权限：仅待处理 Pending 可撤（被选定/已结束的需求不可撤）。
    /// 改动说明（v1.8.2 撤销应答软取消）：撤销由"物理删行"改为"置 Cancelled 保留行"——行仍在，
    /// 故当日应答额度与"每日三件套"的应答项不再回退（兑现"每次应答都计频、撤-重答不绕额度"的承诺，
    /// 此前注释如此写但实现是删行、额度实际会回退）；撤销后重新应答走 Revive 复用原行，不重复占额度/扣分
    /// </summary>
    /// <param name="UserId">操作人（商户成员，员工可代商户撤）</param>
    /// <param name="MerchantId">应答商户</param>
    /// <param name="DemandId">寻货需求</param>
    public record CancelResponseCommand(Guid UserId, Guid MerchantId, Guid DemandId) : IRequest;

    /// <summary>
    /// 撤销应答处理器
    /// </summary>
    public class CancelResponseCommandHandler : IRequestHandler<CancelResponseCommand>
    {
        private readonly ISourcingResponseRepository _responseRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ISourcingDemandRepository _demandRepository;

        /// <summary>
        /// 构造：应答/成员/需求仓储（需求仓储用于撤标后同步递减应答冗余列）
        /// </summary>
        public CancelResponseCommandHandler(
            ISourcingResponseRepository responseRepository,
            IMerchantMemberRepository memberRepository,
            ISourcingDemandRepository demandRepository)
        {
            _responseRepository = responseRepository;
            _memberRepository = memberRepository;
            _demandRepository = demandRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(CancelResponseCommand request, CancellationToken cancellationToken)
        {
            // 撤回资格：操作人须为该商户在职成员（应答是商家行为，员工可代撤）
            var member = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("您不是该商户的在职成员");

            var existing = await _responseRepository.GetByDemandAndMerchantAsync(
                request.DemandId, request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("该需求没有你的应答，无法撤销");

            // 仅待处理可撤：被选定/已结束/已撤销的应答不可再撤（语义矛盾，守卫在聚合内）
            // 改动说明（v1.8.2 撤销应答软取消）：改状态留行，不删行——额度与三件套应答项因此不回退
            existing.Cancel();
            await _responseRepository.UpdateAsync(existing, cancellationToken);

            // 改动说明（v1.8.1 撤标计数维护）：同步递减需求的应答冗余列（同一 UnitOfWork 提交）。
            //   该列驱动 matched/opportunities 的"尚无人应答"推荐，不递减会让单子在其他商家面前永久消失
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken);
            if (demand != null)
            {
                demand.DecrementResponseCount();
                await _demandRepository.UpdateAsync(demand, cancellationToken);
            }
        }
    }
}
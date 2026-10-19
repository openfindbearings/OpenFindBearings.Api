using MediatR;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 撤销应答命令（v1.5.0 取消应答）：商户撤回自己对某需求提交的应答，需求回到未应答状态（比价列表移除该商户）。
    /// 招投标类比"开标前撤标"：仅待处理 Pending 可撤（被选定/已结束的需求不可撤）；当日额度不退还（每次应答都计频，防"撤-重答"绕额度）
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

            // 仅待处理可撤：被选定/已结束的应答不可撤回（语义矛盾）
            if (existing.Status != SourcingResponseStatus.Pending)
                throw new InvalidOperationException("该应答已被处理，无法撤销");

            // 删除应答（型号行 DB 级联删除，UnitOfWork 提交）
            await _responseRepository.RemoveAsync(existing, cancellationToken);

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
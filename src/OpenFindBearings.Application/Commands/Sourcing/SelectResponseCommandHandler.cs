using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 选定应答关闭寻货命令（v1.35.0）：发布人从应答列表确认一家合作——
    /// 需求转 Closed、被选应答 Adopted、其余 Pending 批量 NotSelected、解锁商家联系方式
    /// （改动说明 v2.18.0 电话单向：只解锁"需求方查看商家联系方式"，需求方号码不下发商家，防胁迫回执；
    ///  解锁的读取判定在详情查询：SelectedResponseId 关联才返回 contact 字段）
    /// </summary>
    /// <param name="UserId">操作人（经办人本人，或该商户在职管理员）</param>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="ResponseId">被选定的应答</param>
    /// <param name="ActorMerchantId">操作时的当前商户上下文（商户名义单代办判定用，无则空）</param>
    public record SelectResponseCommand(Guid UserId, Guid DemandId, Guid ResponseId, Guid? ActorMerchantId = null) : IRequest;

    /// <summary>
    /// 选定应答处理器
    /// </summary>
    public class SelectResponseCommandHandler : IRequestHandler<SelectResponseCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly ISourcingResponseRepository _responseRepository;
        private readonly IMerchantRepository _merchantRepository;
        // 改动说明（v2.23.0）：代办判定需校验操作人是否为发布商户的在职管理员
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造：寻货/商户/成员仓储 + 工作单元 + 事件派发
        /// </summary>
        public SelectResponseCommandHandler(
            ISourcingDemandRepository demandRepository,
            ISourcingResponseRepository responseRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _demandRepository = demandRepository;
            _responseRepository = responseRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(SelectResponseCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 改动说明（v2.23.0）：权限由"仅发布人"放宽为"经办人或发布商户在职管理员"，员工离职后仍可处理遗留单
            if (!await DemandAccessGuard.CanManageAsync(
                    demand, request.UserId, request.ActorMerchantId, _memberRepository, cancellationToken))
                throw new UnauthorizedAccessException("只有经办人或该商户管理员可以选定应答");
            demand.TryExpire();
            if (demand.Status != SourcingDemandStatus.Published)
                throw new InvalidOperationException("该寻货已结束，无法选定");

            var selected = await _responseRepository.GetByIdAsync(request.ResponseId, cancellationToken)
                ?? throw new InvalidOperationException("应答不存在");
            if (selected.DemandId != demand.Id)
                throw new InvalidOperationException("应答与需求不匹配");
            if (selected.Status != SourcingResponseStatus.Pending)
                throw new InvalidOperationException("该应答已处理");

            // 选定：目标 Adopted，其余待处理批量 NotSelected（同一需求只成一家）
            selected.Adopt();
            await _responseRepository.UpdateAsync(selected, cancellationToken);
            var pendings = await _responseRepository.GetPendingByDemandAsync(demand.Id, cancellationToken);
            foreach (var other in pendings.Where(p => p.Id != selected.Id))
            {
                other.MarkNotSelected();
                await _responseRepository.UpdateAsync(other, cancellationToken);
            }

            // 订单关闭落地
            var merchant = await _merchantRepository.GetByIdAsync(selected.MerchantId, cancellationToken);
            demand.Select(selected.Id);
            await _demandRepository.UpdateAsync(demand, cancellationToken);

            // 改动说明（选定事件恢复发布）：先显式提交主变更，再 publish 事件——
            // 不依赖 UnitOfWorkBehavior 的 post-save 发布（实测聚合根 AddDomainEvent 后该链路未触发，
            // 成就/通知 handler 全不执行）。此处时序保证：事件处理器在事务提交后运行、独立 SaveChanges，
            // 其失败只回滚自己（积分 xmin 连发已修），不再连坐已提交的主变更。
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _mediator.Publish(new SourcingDemandClosedEvent(
                demand.Id, demand.PublisherUserId, selected.Id, selected.MerchantId,
                merchant?.Name ?? "商户", demand.PartNumber), cancellationToken);
            // 改动说明（v1.6.0 边界预留）：撮合达成（互给电话）——发布预留事件供后续交易域接续；
            //   开源侧无订阅者，仅作衔接锚点
            await _mediator.Publish(new MatchCompletedEvent(
                demand.Id, demand.PublisherUserId, selected.MerchantId, demand.PartNumber), cancellationToken);
        }
    }

    /// <summary>
    /// 取消寻货命令（v1.35.0）：发布人主动关闭进行中的需求（通知全体应答者）
    /// </summary>
    /// <param name="UserId">操作人（经办人本人，或该商户在职管理员）</param>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="ActorMerchantId">操作时的当前商户上下文（商户名义单代办判定用，无则空）</param>
    public record CancelDemandCommand(Guid UserId, Guid DemandId, Guid? ActorMerchantId = null) : IRequest;

    /// <summary>
    /// 取消寻货处理器
    /// </summary>
    public class CancelDemandCommandHandler : IRequestHandler<CancelDemandCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        // 改动说明（v2.23.0）：代办判定需校验操作人是否为发布商户的在职管理员
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造：需求仓储 + 成员仓储 + 工作单元 + 事件派发
        /// </summary>
        public CancelDemandCommandHandler(
            ISourcingDemandRepository demandRepository,
            IMerchantMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _demandRepository = demandRepository;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(CancelDemandCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 改动说明（v2.23.0）：权限由"仅发布人"放宽为"经办人或发布商户在职管理员"
            if (!await DemandAccessGuard.CanManageAsync(
                    demand, request.UserId, request.ActorMerchantId, _memberRepository, cancellationToken))
                throw new UnauthorizedAccessException("只有经办人或该商户管理员可以取消");
            if (demand.Status != SourcingDemandStatus.Published)
                throw new InvalidOperationException("仅进行中的寻货可以取消");

            // 改动说明（同 Select）：显式提交主变更后发布事件，时序与事件隔离同款
            demand.Cancel();
            await _demandRepository.UpdateAsync(demand, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _mediator.Publish(new SourcingDemandCancelledEvent(
                demand.Id, demand.PublisherUserId, demand.PartNumber), cancellationToken);
        }
    }
}
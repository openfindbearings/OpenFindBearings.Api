using MediatR;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 选定应答关闭寻货命令（v1.35.0）：发布人从应答列表确认一家合作——
    /// 需求转 Closed、被选应答 Adopted、其余 Pending 批量 NotSelected、双方联系方式解锁
    /// （解锁的读取判定在详情查询：SelectedResponseId 关联双方才返回 contact 字段）
    /// </summary>
    /// <param name="UserId">发布人（仅本人可操作）</param>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="ResponseId">被选定的应答</param>
    public record SelectResponseCommand(Guid UserId, Guid DemandId, Guid ResponseId) : IRequest;

    /// <summary>
    /// 选定应答处理器
    /// </summary>
    public class SelectResponseCommandHandler : IRequestHandler<SelectResponseCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly ISourcingResponseRepository _responseRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造：寻货/商户仓储 + 事件派发
        /// </summary>
        public SelectResponseCommandHandler(
            ISourcingDemandRepository demandRepository,
            ISourcingResponseRepository responseRepository,
            IMerchantRepository merchantRepository,
            IMediator mediator)
        {
            _demandRepository = demandRepository;
            _responseRepository = responseRepository;
            _merchantRepository = merchantRepository;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(SelectResponseCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            if (demand.PublisherUserId != request.UserId)
                throw new UnauthorizedAccessException("只有发布人可以选定应答");
            demand.TryExpire();
            if (demand.Status != SourcingDemand.StatusPublished)
                throw new InvalidOperationException("该寻货已结束，无法选定");

            var selected = await _responseRepository.GetByIdAsync(request.ResponseId, cancellationToken)
                ?? throw new InvalidOperationException("应答不存在");
            if (selected.DemandId != demand.Id)
                throw new InvalidOperationException("应答与需求不匹配");
            if (selected.Status != SourcingResponse.StatusPending)
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

            demand.Select(selected.Id);
            await _demandRepository.UpdateAsync(demand, cancellationToken);

            var merchant = await _merchantRepository.GetByIdAsync(selected.MerchantId, cancellationToken);
            await _mediator.Publish(new SourcingDemandClosedEvent(
                demand.Id, demand.PublisherUserId, selected.Id, selected.MerchantId,
                merchant?.Name ?? "商户", demand.PartNumber), cancellationToken);
        }
    }

    /// <summary>
    /// 取消寻货命令（v1.35.0）：发布人主动关闭进行中的需求（通知全体应答者）
    /// </summary>
    /// <param name="UserId">发布人</param>
    /// <param name="DemandId">寻货需求</param>
    public record CancelDemandCommand(Guid UserId, Guid DemandId) : IRequest;

    /// <summary>
    /// 取消寻货处理器
    /// </summary>
    public class CancelDemandCommandHandler : IRequestHandler<CancelDemandCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造：需求仓储 + 事件派发
        /// </summary>
        public CancelDemandCommandHandler(ISourcingDemandRepository demandRepository, IMediator mediator)
        {
            _demandRepository = demandRepository;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(CancelDemandCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            if (demand.PublisherUserId != request.UserId)
                throw new UnauthorizedAccessException("只有发布人可以取消");
            if (demand.Status != SourcingDemand.StatusPublished)
                throw new InvalidOperationException("仅进行中的寻货可以取消");

            demand.Cancel();
            await _demandRepository.UpdateAsync(demand, cancellationToken);
            await _mediator.Publish(new SourcingDemandCancelledEvent(
                demand.Id, demand.PublisherUserId, demand.PartNumber), cancellationToken);
        }
    }
}

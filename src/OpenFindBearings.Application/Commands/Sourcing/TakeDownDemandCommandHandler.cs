using MediatR;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 平台下架寻货命令（v1.35.0，Admin 治理）：违规单软下架（保留行可追溯），
    /// 通知发布人附原因。状态守卫：仅进行中/已过期可下架（已关闭单无治理必要）
    /// </summary>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="Reason">下架原因（透传给发布人）</param>
    public record TakeDownDemandCommand(Guid DemandId, string? Reason) : IRequest;

    /// <summary>
    /// 下架寻货处理器
    /// </summary>
    public class TakeDownDemandCommandHandler : IRequestHandler<TakeDownDemandCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造：需求仓储 + 事件派发
        /// </summary>
        public TakeDownDemandCommandHandler(ISourcingDemandRepository demandRepository, IMediator mediator)
        {
            _demandRepository = demandRepository;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(TakeDownDemandCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            if (demand.Status is not (SourcingDemand.StatusPublished or SourcingDemand.StatusExpired))
                throw new InvalidOperationException("仅进行中或已过期的寻货需要下架");

            demand.TakeDown();
            await _demandRepository.UpdateAsync(demand, cancellationToken);
            await _mediator.Publish(new SourcingDemandTakenDownEvent(
                demand.Id, demand.PublisherUserId, demand.PartNumber, request.Reason), cancellationToken);
        }
    }
}

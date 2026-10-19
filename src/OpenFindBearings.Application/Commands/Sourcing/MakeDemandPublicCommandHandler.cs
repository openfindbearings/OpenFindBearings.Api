using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 转为公开招标命令（v1.8.0 公开招标/邀标）：邀标单进行中时由发布人放开为公开
    /// （需求进公开大厅，人人可见可应答）。解决"邀标后无人应答只能取消重建"的损耗
    /// （原定向单只能取消再发，发布额度与积分不退还）。
    /// </summary>
    /// <param name="DemandId">寻货单 ID</param>
    /// <param name="UserId">操作人（仅发布人本人可操作，与列表管理按钮口径一致）</param>
    public record MakeDemandPublicCommand(Guid DemandId, Guid UserId) : IRequest;

    /// <summary>
    /// 转为公开招标处理器（守卫在聚合内：已是公开/已非进行中一律拒绝）
    /// </summary>
    public class MakeDemandPublicCommandHandler : IRequestHandler<MakeDemandPublicCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;

        /// <summary>
        /// 构造：需求仓储
        /// </summary>
        public MakeDemandPublicCommandHandler(ISourcingDemandRepository demandRepository)
        {
            _demandRepository = demandRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(MakeDemandPublicCommand request, CancellationToken cancellationToken)
        {
            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 权限：仅发布人本人（商户名义单同样限经办人——与"商户寻货列表仅本人出管理按钮"口径一致）
            if (demand.PublisherUserId != request.UserId)
                throw new UnauthorizedAccessException("只有发布人可以将寻货转为公开招标");

            demand.MakePublic();
            await _demandRepository.UpdateAsync(demand, cancellationToken);
        }
    }
}

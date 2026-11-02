using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 批量删除寻货需求命令（v2.12.0 列表删除）：发布方软删自己的终态单，清单整理用。
    /// 守卫逐条执行——仅经办人本人或该商户在职管理员 + 仅终态（进行中拒删，须先取消走通知流程）；
    /// 单条失败不中断整批，返回成功/跳过计数
    /// </summary>
    /// <param name="UserId">操作人（须为各单经办人，或发布商户在职管理员）</param>
    /// <param name="Ids">待删需求 ID 集</param>
    /// <param name="ActorMerchantId">操作时的当前商户上下文（商户名义单代办判定用，无则空）</param>
    public record BatchDeleteDemandsCommand(Guid UserId, List<Guid> Ids, Guid? ActorMerchantId = null)
        : IRequest<(int Deleted, int Skipped)>;

    /// <summary>
    /// 批量删除处理器：逐条取需求→校验归属与状态→软删（IsDeleted），数据与应答方视图保留
    /// </summary>
    public class BatchDeleteDemandsCommandHandler : IRequestHandler<BatchDeleteDemandsCommand, (int Deleted, int Skipped)>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        // 改动说明（v2.23.0）：代办判定需校验操作人是否为发布商户在职管理员
        private readonly IMerchantMemberRepository _memberRepository;

        /// <summary>构造：需求仓储 + 商户成员仓储</summary>
        public BatchDeleteDemandsCommandHandler(
            ISourcingDemandRepository demandRepository,
            IMerchantMemberRepository memberRepository)
        {
            _demandRepository = demandRepository;
            _memberRepository = memberRepository;
        }

        /// <inheritdoc/>
        public async Task<(int Deleted, int Skipped)> Handle(BatchDeleteDemandsCommand request, CancellationToken cancellationToken)
        {
            var deleted = 0;
            var skipped = 0;
            foreach (var id in request.Ids.Distinct())
            {
                var demand = await _demandRepository.GetByIdAsync(id, cancellationToken);
                // 归属守卫：非经办人且非发布商户管理员时静默跳过（不泄露存在性）
                // 改动说明（v2.23.0）：由"仅经办人"放宽为统一守卫，员工离职后管理员可清理遗留终态单
                if (demand is null || !await DemandAccessGuard.CanManageAsync(
                        demand, request.UserId, request.ActorMerchantId, _memberRepository, cancellationToken))
                {
                    skipped++;
                    continue;
                }
                try
                {
                    demand.Delete();
                    await _demandRepository.UpdateAsync(demand, cancellationToken);
                    deleted++;
                }
                catch (InvalidOperationException)
                {
                    // 进行中单拒删（须先取消）——计入跳过，不中断整批
                    skipped++;
                }
            }
            return (deleted, skipped);
        }
    }
}

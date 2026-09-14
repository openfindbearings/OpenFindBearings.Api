using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.WithdrawApplication
{
    /// <summary>
    /// 申请人自助撤回入驻申请命令。仅当商户仍处于 Pending 且调用者是其当前在职 MerchantAdmin 成员
    /// （即申请人本人）时允许执行；撤回清理策略按 ApplicationMode 分支：
    ///   Self 新建 → 硬删除商户本体 + 其成员/执照记录（未公示、避免僵尸数据干扰同名再新建查重）；
    ///   Claim 认领 → 不删商户，仅解除认领人的 MerchantAdmin 成员关系并把 DataSource 退回 Crawler，
    ///     让商户重新进入认领池并可被 Sync 覆盖；
    ///   Nomination → 本入口不覆盖（提名发起方不是当前商户的成员，不出现在其"申请列表"中）。
    /// </summary>
    public record WithdrawApplicationCommand : IRequest, ICommand
    {
        /// <summary>
        /// 待撤回的申请所属商户ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 调用者（申请人）用户ID
        /// </summary>
        public Guid ApplicantUserId { get; init; }
    }
}

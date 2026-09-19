using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.DeleteRejectedApplication
{
    /// <summary>
    /// 申请人删除被驳回的入驻申请命令（v2.6.0 新增）。
    /// 守卫：商户处于 Suspended（仅被驳回的申请可删，审核中走 withdraw、生效后属正式商户）
    /// 且调用者是在职 MerchantAdmin（申请人本人）。
    /// 清理分支与撤回同构（ApplicantApplicationCleanup）：Self 硬删商户+成员、
    /// Claim 解除认领人并退回爬虫认领池、Nomination/None 拒绝。
    /// </summary>
    public record DeleteRejectedApplicationCommand : IRequest, ICommand
    {
        /// <summary>
        /// 待删除的被拒申请所属商户ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 调用者（申请人）用户ID
        /// </summary>
        public Guid ApplicantUserId { get; init; }
    }
}

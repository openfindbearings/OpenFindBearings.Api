using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.StaffInvitationActions
{
    /// <summary>
    /// 拒绝员工邀请（v2.9.0 邀请确认制：被邀人拒绝，邀请置 Declined，不建成员行）
    /// </summary>
    public record DeclineStaffInvitationCommand : IRequest<bool>, ICommand
    {
        /// <summary>
        /// 邀请ID
        /// </summary>
        public Guid InvitationId { get; init; }

        /// <summary>
        /// 当前用户业务ID（v2.11.0：拒绝时核销本人邀请站内信用）
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 当前用户手机号（JWT claim，须与邀请目标一致）
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 当前用户邮箱（JWT claim，无手机号邀请的兜底匹配）
        /// </summary>
        public string? Email { get; init; }
    }

    /// <summary>
    /// 撤销员工邀请（管理员撤回自己发出的待确认邀请）
    /// </summary>
    public record RevokeStaffInvitationCommand : IRequest<bool>, ICommand
    {
        /// <summary>
        /// 邀请ID
        /// </summary>
        public Guid InvitationId { get; init; }

        /// <summary>
        /// 操作人业务ID（须为该商户在职管理员）
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}

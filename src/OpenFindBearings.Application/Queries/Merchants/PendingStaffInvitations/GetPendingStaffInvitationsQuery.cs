using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Merchants.PendingStaffInvitations
{
    /// <summary>
    /// 待我确认的员工邀请查询（v2.9.0 邀请确认制：被邀人登录后按 JWT 手机号/邮箱匹配，
    /// 商户页横幅展示，同意/拒绝后消失）
    /// </summary>
    public record GetPendingStaffInvitationsQuery : IRequest<List<PendingStaffInvitationDto>>, IQuery
    {
        /// <summary>
        /// 被邀人手机号（取自 JWT phone_number claim，仅能查发给自己的邀请）
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 被邀人邮箱（取自 JWT email claim，无手机号用户的兜底匹配）
        /// </summary>
        public string? Email { get; init; }
    }

    /// <summary>
    /// 待确认员工邀请摘要
    /// </summary>
    /// <param name="InvitationId">邀请ID（接受/拒绝用）</param>
    /// <param name="MerchantId">商户ID</param>
    /// <param name="MerchantName">商户名称</param>
    /// <param name="Role">入伙角色（MerchantAdmin/MerchantStaff）</param>
    /// <param name="InvitedByName">发起人昵称</param>
    /// <param name="CreatedAt">发出时间</param>
    public record PendingStaffInvitationDto(
        Guid InvitationId,
        Guid MerchantId,
        string MerchantName,
        string Role,
        string? InvitedByName,
        DateTime CreatedAt);
}

using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.StaffInvitationActions
{
    /// <summary>
    /// 接受员工邀请（v2.9.0 邀请确认制：被邀人同意后建成员行入伙）。
    /// Phone 必须来自服务端 JWT phone_number claim——防拿他人手机号撞领邀请。
    /// </summary>
    public record AcceptStaffInvitationCommand : IRequest<bool>, ICommand
    {
        /// <summary>
        /// 邀请ID
        /// </summary>
        public Guid InvitationId { get; init; }

        /// <summary>
        /// 当前用户业务ID（API User.id）
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 当前用户认证ID（Identity sub，写入邀请完成记录）
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 当前用户手机号（JWT claim，须与邀请目标手机号一致）
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 当前用户邮箱（JWT claim，无手机号邀请的兜底匹配）
        /// </summary>
        public string? Email { get; init; }
    }
}

using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.DeactivateUser
{
    /// <summary>
    /// 注销当前账户命令（v2.12.0）：软删业务用户 + 商户关系清理 + Identity 禁用吊销。
    /// 守卫与清理规则（对标主流平台）：
    /// 唯一在职管理员的生效商户存在时拒绝注销（防无主商户）；其余成员行移除；
    /// 审核中/被拒的自助申请按撤回逻辑清理；待确认邀请作废；通知清空；
    /// 30 天冷静期到期后由 UserDeactivationJob 匿名化 PII。
    /// </summary>
    public record DeactivateUserCommand : IRequest, ICommand
    {
        /// <summary>
        /// 业务用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// Identity 主体ID（注销认证账户与吊销令牌用）
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 手机号（作废发给本人的待确认邀请；来自 JWT claim）
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 邮箱（同上）
        /// </summary>
        public string? Email { get; init; }
    }
}

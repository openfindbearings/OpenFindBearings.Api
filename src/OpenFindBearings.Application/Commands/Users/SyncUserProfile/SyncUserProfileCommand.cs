using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.SyncUserProfile
{
    /// <summary>
    /// 同步用户资料缓存（v2.11.0 建，v2.11.1 扩展）：登录中间件按 JWT claim 回填/刷新
    /// User.Mobile 与 User.Nickname，供同商户成员详情展示。Identity 是事实源，本列仅缓存副本。
    /// 改动说明（v2.11.1）：原名 SyncUserMobileCommand，加昵称回填后更名——
    ///   建号时 Name claim 可能为空导致成员详情"未命名"，中间件按兜底链持续补写。
    /// </summary>
    public record SyncUserProfileCommand : IRequest, ICommand
    {
        /// <summary>
        /// 业务用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// claim 中的手机号（null 表示本次登录无手机 claim，不覆盖既有缓存）
        /// </summary>
        public string? Mobile { get; init; }

        /// <summary>
        /// claim 中的昵称兜底链值（Name→preferred_username→手机号；仅当前昵称为空时补写，不覆盖自改值）
        /// </summary>
        public string? Nickname { get; init; }
    }
}

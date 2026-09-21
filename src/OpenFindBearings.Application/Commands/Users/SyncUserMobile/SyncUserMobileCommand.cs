using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.SyncUserMobile
{
    /// <summary>
    /// 同步用户手机号缓存（v2.11.0）：登录中间件按 JWT phone_number claim 回填/刷新 User.Mobile，
    /// 供同商户成员详情展示。Identity 是手机号事实源，本列仅缓存副本。
    /// </summary>
    public record SyncUserMobileCommand : IRequest, ICommand
    {
        /// <summary>
        /// 业务用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// claim 中的手机号（null 表示本次登录无手机 claim，不覆盖既有缓存）
        /// </summary>
        public string? Mobile { get; init; }
    }
}

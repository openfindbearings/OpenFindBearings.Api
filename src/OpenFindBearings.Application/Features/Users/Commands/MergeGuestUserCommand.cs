using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 合并游客数据到正式账户命令
    /// </summary>
    public record MergeGuestUserCommand : IRequest
    {
        /// <summary>
        /// 认证用户ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 游客会话ID
        /// </summary>
        public string GuestSessionId { get; init; } = string.Empty;
    }
}

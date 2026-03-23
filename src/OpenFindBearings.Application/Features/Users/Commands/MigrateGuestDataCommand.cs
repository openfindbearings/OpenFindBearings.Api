using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 迁移游客数据命令
    /// 当游客登录后，自动将SessionId关联的数据迁移到正式账户
    /// </summary>
    public record MigrateGuestDataCommand : IRequest
    {
        /// <summary>
        /// 游客会话ID
        /// </summary>
        public string GuestSessionId { get; init; } = string.Empty;

        /// <summary>
        /// 目标用户ID（正式账户）
        /// </summary>
        public Guid TargetUserId { get; init; }
    }
}

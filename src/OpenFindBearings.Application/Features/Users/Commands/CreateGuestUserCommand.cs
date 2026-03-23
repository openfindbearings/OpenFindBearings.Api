using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 创建游客用户命令
    /// </summary>
    public record CreateGuestUserCommand : IRequest<Guid>
    {
        /// <summary>
        /// 游客会话ID
        /// </summary>
        public string SessionId { get; init; } = string.Empty;

        public CreateGuestUserCommand(string sessionId)
        {
            SessionId = sessionId;
        }
    }
}

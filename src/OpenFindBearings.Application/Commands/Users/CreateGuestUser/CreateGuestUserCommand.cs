using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.Commands
{
    /// <summary>
    /// 创建游客用户命令
    /// </summary>
    public record CreateGuestUserCommand : IRequest<Guid>, ICommand
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

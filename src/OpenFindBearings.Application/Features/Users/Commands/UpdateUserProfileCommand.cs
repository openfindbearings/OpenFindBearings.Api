using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 更新用户资料命令
    /// </summary>
    public record UpdateUserProfileCommand : IRequest
    {
        public string UserId { get; init; } = string.Empty; // AuthUserId
        public string? Nickname { get; init; }
        public string? Avatar { get; init; }
        public string? Phone { get; init; }
        public string? Address { get; init; }
    }
}

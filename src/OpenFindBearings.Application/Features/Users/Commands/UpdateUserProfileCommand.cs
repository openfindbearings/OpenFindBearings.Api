using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 更新用户资料命令
    /// </summary>
    public record UpdateUserProfileCommand : IRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; init; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string? Avatar { get; init; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; init; }
    }
}

using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 更新用户类型命令
    /// </summary>
    public record UpdateUserTypeCommand : IRequest
    {
        /// <summary>
        /// 用户认证ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 新的用户类型
        /// </summary>
        public UserType UserType { get; init; }
    }
}

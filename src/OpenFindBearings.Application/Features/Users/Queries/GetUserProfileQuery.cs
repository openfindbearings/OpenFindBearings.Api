using MediatR;
using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 获取用户详细信息（包含统计信息）查询
    /// </summary>
    public record GetUserProfileQuery : IRequest<UserDto?>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

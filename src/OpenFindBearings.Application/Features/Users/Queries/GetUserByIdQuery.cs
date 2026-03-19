using MediatR;
using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 根据用户ID获取用户基础信息查询
    /// </summary>
    public record GetUserByIdQuery : IRequest<UserDto?>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

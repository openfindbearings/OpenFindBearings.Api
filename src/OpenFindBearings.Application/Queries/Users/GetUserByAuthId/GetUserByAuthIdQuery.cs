using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Users.GetUserByAuthId
{
    /// <summary>
    /// 根据认证ID获取用户查询
    /// </summary>
    public record GetUserByAuthIdQuery : IRequest<UserDto?>, IQuery
    {
        /// <summary>
        /// 认证系统用户ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;
    }
}

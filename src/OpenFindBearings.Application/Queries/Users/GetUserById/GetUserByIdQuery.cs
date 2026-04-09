using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Queries
{
    /// <summary>
    /// 根据用户ID获取用户基础信息查询
    /// </summary>
    public record GetUserByIdQuery : IRequest<UserDto?>, IQuery
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

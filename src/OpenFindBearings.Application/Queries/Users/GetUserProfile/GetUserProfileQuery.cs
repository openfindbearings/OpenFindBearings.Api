using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Queries
{
    /// <summary>
    /// 获取用户详细信息（包含统计信息）查询
    /// </summary>
    public record GetUserProfileQuery : IRequest<UserDto?>, IQuery
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Users.GetUserRoles
{
    /// <summary>
    /// 获取用户角色列表查询
    /// </summary>
    public record GetUserRolesQuery : IRequest<List<string>>, IQuery
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

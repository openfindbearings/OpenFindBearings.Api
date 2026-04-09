using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Users.GetUserPermissions
{
    /// <summary>
    /// 获取用户权限列表查询
    /// </summary>
    public record GetUserPermissionsQuery : IRequest<List<string>>, IQuery
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

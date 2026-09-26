using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Users.GetAllPlatformRoles
{
    /// <summary>
    /// 获取全部用户的平台角色映射查询（v1.38.0，Admin 用户列表角色列合并用）
    /// </summary>
    public record GetAllPlatformRolesQuery : IRequest<Dictionary<string, List<string>>>, IQuery
    {
    }
}

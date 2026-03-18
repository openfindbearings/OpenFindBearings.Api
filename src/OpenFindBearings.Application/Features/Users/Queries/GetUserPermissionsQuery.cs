using MediatR;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 获取用户权限列表查询
    /// </summary>
    public record GetUserPermissionsQuery(string AuthUserId) : IRequest<List<string>>;
}

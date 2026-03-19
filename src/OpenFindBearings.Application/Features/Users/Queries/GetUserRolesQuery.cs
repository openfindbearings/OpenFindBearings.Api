using MediatR;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 获取用户角色列表查询
    /// </summary>
    public record GetUserRolesQuery : IRequest<List<string>>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

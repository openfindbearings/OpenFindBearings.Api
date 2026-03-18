using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 获取角色权限列表查询处理器
    /// </summary>
    public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<string>>
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly ILogger<GetRolePermissionsQueryHandler> _logger;

        public GetRolePermissionsQueryHandler(
            IRolePermissionRepository rolePermissionRepository,
            ILogger<GetRolePermissionsQueryHandler> logger)
        {
            _rolePermissionRepository = rolePermissionRepository;
            _logger = logger;
        }

        public async Task<List<string>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取角色权限列表: RoleId={RoleId}", request.RoleId);

            // 这里需要 RolePermissionRepository 提供 GetPermissionsByRoleId 方法
            var permissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(request.RoleId, cancellationToken);
            return permissions.Select(p => p.Name).ToList();

            //// 临时返回空列表
            //return new List<string>();
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Roles.AssignPermissionsToRole
{
    /// <summary>
    /// 分配权限给角色命令处理器
    /// </summary>
    public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly ILogger<AssignPermissionsToRoleCommandHandler> _logger;

        public AssignPermissionsToRoleCommandHandler(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            ILogger<AssignPermissionsToRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _logger = logger;
        }

        public async Task Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("分配权限给角色: RoleId={RoleId}, 权限数量={PermissionCount}",
                request.RoleId, request.PermissionNames.Count);

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"角色不存在: {request.RoleId}");
            }

            // 清空原有权限
            await _rolePermissionRepository.ClearRolePermissionsAsync(request.RoleId, cancellationToken);

            // 分配新权限
            foreach (var permissionName in request.PermissionNames)
            {
                var permission = await _permissionRepository.GetByNameAsync(permissionName, cancellationToken);
                if (permission != null)
                {
                    await _rolePermissionRepository.AddPermissionToRoleAsync(
                        request.RoleId, permission.Id, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("权限不存在: {PermissionName}", permissionName);
                }
            }

            _logger.LogInformation("权限分配成功: RoleId={RoleId}", request.RoleId);
        }
    }
}

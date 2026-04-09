using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Permissions.DeletePermission
{
    /// <summary>
    /// 删除权限命令处理器
    /// </summary>
    public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<DeletePermissionCommandHandler> _logger;

        public DeletePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            ILogger<DeletePermissionCommandHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("删除权限: {PermissionId}", request.Id);

            var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (permission == null)
            {
                throw new InvalidOperationException($"权限不存在: {request.Id}");
            }

            // 检查权限是否被使用
            if (permission.RolePermissions.Any())
            {
                throw new InvalidOperationException($"权限已被角色使用，无法删除: {permission.Name}");
            }

            await _permissionRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("权限删除成功: {PermissionId}", request.Id);
        }
    }
}

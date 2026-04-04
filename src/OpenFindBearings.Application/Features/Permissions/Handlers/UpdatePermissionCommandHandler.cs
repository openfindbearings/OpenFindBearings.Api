using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Permissions.Handlers
{
    /// <summary>
    /// 更新权限命令处理器
    /// </summary>
    public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<UpdatePermissionCommandHandler> _logger;

        public UpdatePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            ILogger<UpdatePermissionCommandHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新权限: {PermissionId}", request.Id);

            var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (permission == null)
            {
                throw new InvalidOperationException($"权限不存在: {request.Id}");
            }

            // 如果名称有变化，检查是否与其他权限冲突
            if (permission.Name != request.Name)
            {
                var existing = await _permissionRepository.GetByNameAsync(request.Name, cancellationToken);
                if (existing != null && existing.Id != request.Id)
                {
                    throw new InvalidOperationException($"权限名已存在: {request.Name}");
                }
            }

            permission.UpdateDescription(request.Description);
            // 如果需要更新名称，需要 Permission 实体添加 UpdateName 方法
            // permission.UpdateName(request.Name);

            await _permissionRepository.UpdateAsync(permission, cancellationToken);

            _logger.LogInformation("权限更新成功: {PermissionId}", permission.Id);
        }
    }
}

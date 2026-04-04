using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Permissions.Handlers
{
    /// <summary>
    /// 创建权限命令处理器
    /// </summary>
    public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Guid>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<CreatePermissionCommandHandler> _logger;

        public CreatePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            ILogger<CreatePermissionCommandHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建权限: {PermissionName}", request.Name);

            // 检查权限名是否已存在
            var existing = await _permissionRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"权限名已存在: {request.Name}");
            }

            var permission = new Permission(request.Name, request.Description);
            await _permissionRepository.AddAsync(permission, cancellationToken);

            _logger.LogInformation("权限创建成功: {PermissionId}, 名称: {PermissionName}",
                permission.Id, permission.Name);

            return permission.Id;
        }
    }
}

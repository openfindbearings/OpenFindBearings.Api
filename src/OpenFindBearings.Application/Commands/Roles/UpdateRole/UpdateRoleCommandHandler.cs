using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Roles.UpdateRole
{
    /// <summary>
    /// 更新角色命令处理器
    /// </summary>
    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(
            IRoleRepository roleRepository,
            ILogger<UpdateRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新角色: {RoleId}", request.Id);

            var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"角色不存在: {request.Id}");
            }

            // 如果名称有变化，检查是否与其他角色冲突
            if (role.Name != request.Name)
            {
                var exists = await _roleRepository.ExistsAsync(request.Name, cancellationToken);
                if (exists)
                {
                    throw new InvalidOperationException($"角色名已存在: {request.Name}");
                }
            }

            role.UpdateDescription(request.Description);
            // 改动说明（v1.38.0）：显示名可编辑（中文自由），Name 机器标识保持不可改（鉴权键稳定）
            role.UpdateDisplayName(request.DisplayName);

            await _roleRepository.UpdateAsync(role, cancellationToken);

            _logger.LogInformation("角色更新成功: {RoleId}", role.Id);
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Roles.DeleteRole
{
    /// <summary>
    /// 删除角色命令处理器
    /// </summary>
    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(
            IRoleRepository roleRepository,
            ILogger<DeleteRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("删除角色: {RoleId}", request.Id);

            var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"角色不存在: {request.Id}");
            }

            // 检查是否为系统内置角色（不可删除）
            // 改动说明（v1.39.0）：从硬编码名字名单改为读 Role.IsSystem 字段——
            //   字段本就是"内置不可删"的设计载体（SeedData 落 true），此前写了没人读；
            //   接通后管理员新建的自定义角色可删、内置四角色受字段保护，名单退役
            if (role.IsSystem)
            {
                throw new InvalidOperationException($"系统角色不可删除: {role.Name}");
            }

            await _roleRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("角色删除成功: {RoleId}", request.Id);
        }
    }
}

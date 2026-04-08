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

            // 检查是否为系统角色（不可删除）
            if (role.Name == "GlobalAdmin" || role.Name == "MerchantAdmin")
            {
                throw new InvalidOperationException($"系统角色不可删除: {role.Name}");
            }

            await _roleRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("角色删除成功: {RoleId}", request.Id);
        }
    }
}

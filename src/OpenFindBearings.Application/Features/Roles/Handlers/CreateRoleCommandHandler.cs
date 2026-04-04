using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 创建角色命令处理器
    /// </summary>
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository,
            ILogger<CreateRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建角色: {RoleName}", request.Name);

            // 检查角色名是否已存在
            var exists = await _roleRepository.ExistsAsync(request.Name, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"角色名已存在: {request.Name}");
            }

            var role = new Role(request.Name, request.Description);
            await _roleRepository.AddAsync(role, cancellationToken);

            _logger.LogInformation("角色创建成功: {RoleId}, 名称: {RoleName}", role.Id, role.Name);

            return role.Id;
        }
    }
}

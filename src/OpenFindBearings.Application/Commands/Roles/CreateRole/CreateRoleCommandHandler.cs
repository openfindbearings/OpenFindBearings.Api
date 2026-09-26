using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Roles.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Roles.CreateRole
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

            // 改动说明（v1.38.0）：Name 是鉴权链路里的机器标识（cookie claims/策略比对），
            // 限英文标识防中文误填进 Name 导致 claims 比对面炸掉；中文名走 DisplayName
            if (string.IsNullOrWhiteSpace(request.Name) || !System.Text.RegularExpressions.Regex.IsMatch(request.Name, "^[A-Za-z][A-Za-z0-9_]*$"))
            {
                throw new InvalidOperationException("角色标识仅允许英文字母开头，由字母/数字/下划线组成；中文名称请填“显示名称”");
            }

            // 检查角色名是否已存在
            var exists = await _roleRepository.ExistsAsync(request.Name, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"角色名已存在: {request.Name}");
            }

            var role = new Role(request.Name, request.Description, false, request.DisplayName);
            await _roleRepository.AddAsync(role, cancellationToken);

            _logger.LogInformation("角色创建成功: {RoleId}, 名称: {RoleName}", role.Id, role.Name);

            return role.Id;
        }
    }
}

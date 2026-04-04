using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 分配角色给用户命令处理器
    /// </summary>
    public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

        public AssignRoleToUserCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<AssignRoleToUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("分配角色给用户: UserId={UserId}, Role={RoleName}",
                request.UserId, request.RoleName);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"角色不存在: {request.RoleName}");
            }

            await _userRoleRepository.AddUserToRoleAsync(user.Id, role.Id, cancellationToken);

            _logger.LogInformation("角色分配成功: UserId={UserId}, RoleId={RoleId}",
                user.Id, role.Id);
        }
    }
}

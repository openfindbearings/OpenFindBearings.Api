using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 从用户移除角色命令处理器
    /// </summary>
    public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

        public RemoveRoleFromUserCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<RemoveRoleFromUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("从用户移除角色: AuthUserId={AuthUserId}, Role={RoleName}",
                request.AuthUserId, request.RoleName);

            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.AuthUserId}");
            }

            var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"角色不存在: {request.RoleName}");
            }

            await _userRoleRepository.RemoveUserFromRoleAsync(user.Id, role.Id, cancellationToken);

            _logger.LogInformation("角色移除成功: UserId={UserId}, RoleId={RoleId}",
                user.Id, role.Id);
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Users.ProvisionUser
{
    /// <summary>
    /// 预置业务用户处理器：按 Identity sub find-or-create API 侧 User 行，
    /// 再逐个解析角色名挂到该用户（幂等：已有行/已有角色均跳过不报错）。
    /// 改动说明（v1.38.0）：解决"后台新建账号未登录过 app → 无 User 行 → 平台角色分配 404"死结
    /// </summary>
    public class ProvisionUserCommandHandler : IRequestHandler<ProvisionUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<ProvisionUserCommandHandler> _logger;

        public ProvisionUserCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ILogger<ProvisionUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(ProvisionUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AuthUserId) || !Guid.TryParse(request.AuthUserId, out _))
            {
                throw new InvalidOperationException("AuthUserId 必须是合法的 Identity 用户 GUID");
            }

            // find-or-create：行已存在（用户已登录过或被重复预置）则复用，仅继续补角色
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                user = User.CreateFromAuth(request.AuthUserId, RegistrationSource.Admin,
                    nickname: string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim());
                await _userRepository.AddAsync(user, cancellationToken);
                _logger.LogInformation("预置业务用户行: AuthUserId={Auth} Nickname={Nick}", request.AuthUserId, user.Nickname);
            }

            // 挂角色：按名解析，未知角色名跳过并记日志（不整单失败）；AddRole 域方法自身幂等
            foreach (var roleName in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
                if (role == null)
                {
                    _logger.LogWarning("预置用户时跳过未知角色: {RoleName}", roleName);
                    continue;
                }
                user.AddRole(role.Id);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            return user.Id;
        }
    }
}

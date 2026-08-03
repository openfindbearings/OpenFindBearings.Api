using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Users.CreateUserFromAuth
{
    /// <summary>
    /// 从认证服务创建业务用户命令处理器
    /// </summary>
    public class CreateUserFromAuthCommandHandler : IRequestHandler<CreateUserFromAuthCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<CreateUserFromAuthCommandHandler> _logger;

        public CreateUserFromAuthCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IStaffInvitationRepository invitationRepository,
            ILogger<CreateUserFromAuthCommandHandler> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateUserFromAuthCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建业务用户: AuthUserId={AuthUserId}, RegistrationSource={RegistrationSource}, Nickname={Nickname}",
                request.AuthUserId, request.RegistrationSource, request.Nickname);

            var existingUser = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("用户已存在: AuthUserId={AuthUserId}, UserId={UserId}",
                    request.AuthUserId, existingUser.Id);
                return existingUser.Id;
            }

            var user = new User(
                authUserId: request.AuthUserId,
                registrationSource: request.RegistrationSource,
                registerIp: request.RegisterIp,
                nickname: request.Nickname
            );

            await _userRepository.AddAsync(user, cancellationToken);

            // 处理员工邀请码
            if (!string.IsNullOrEmpty(request.InviteCode))
            {
                try
                {
                    var invitation = await _invitationRepository.GetByCodeAsync(request.InviteCode, cancellationToken);
                    if (invitation != null && !invitation.IsCompleted && !invitation.IsExpired())
                    {
                        user.AssignToMerchant(invitation.MerchantId);
                        invitation.Complete(request.AuthUserId);
                        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
                        _logger.LogInformation("员工邀请码已处理: UserId={UserId}, MerchantId={MerchantId}",
                            user.Id, invitation.MerchantId);
                    }
                    else
                    {
                        _logger.LogWarning("邀请码无效或已过期: InviteCode={InviteCode}", request.InviteCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理员工邀请码异常: InviteCode={InviteCode}", request.InviteCode);
                }
            }

            // 确定角色：预置管理员直接分配 Admin 角色，其余用户分配 Individual
            var roleName = user.AuthUserId == ServiceConstants.BusinessAdminAuthUserId
                ? "Admin"
                : "Individual";
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role != null)
            {
                user.AddRole(role.Id);
                _logger.LogInformation("已为用户 {UserId} 分配角色 {RoleName}", user.Id, role.Name);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("业务用户创建成功: UserId={UserId}, AuthUserId={AuthUserId}",
                user.Id, user.AuthUserId);

            return user.Id;
        }
    }
}

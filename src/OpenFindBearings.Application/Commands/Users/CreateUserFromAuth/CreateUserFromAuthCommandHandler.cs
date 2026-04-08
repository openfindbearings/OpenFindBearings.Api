using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CreateUserFromAuthCommandHandler> _logger;

        public CreateUserFromAuthCommandHandler(
            IUserRepository userRepository,
            ILogger<CreateUserFromAuthCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateUserFromAuthCommand request, CancellationToken cancellationToken)
        {
            // ✅ 修改：移除 UserType
            _logger.LogInformation("创建业务用户: AuthUserId={AuthUserId}, RegistrationSource={RegistrationSource}, Nickname={Nickname}",
                request.AuthUserId, request.RegistrationSource, request.Nickname);

            // 检查是否已存在
            var existingUser = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("用户已存在: AuthUserId={AuthUserId}, UserId={UserId}",
                    request.AuthUserId, existingUser.Id);
                return existingUser.Id;
            }

            // ✅ 修改：移除 userType 参数
            var user = new User(
                authUserId: request.AuthUserId,
                registrationSource: request.RegistrationSource,
                registerIp: request.RegisterIp,
                nickname: request.Nickname
            );

            await _userRepository.AddAsync(user, cancellationToken);

            _logger.LogInformation("业务用户创建成功: UserId={UserId}, AuthUserId={AuthUserId}",
                user.Id, user.AuthUserId);

            return user.Id;
        }
    }
}

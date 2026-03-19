using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 更新用户类型命令处理器
    /// </summary>
    public class UpdateUserTypeCommandHandler : IRequestHandler<UpdateUserTypeCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserTypeCommandHandler> _logger;

        public UpdateUserTypeCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserTypeCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateUserTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新用户类型: UserId={UserId}, NewType={UserType}",
                request.UserId, request.UserType);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            user.UpdateUserType(request.UserType);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("用户类型更新成功: UserId={UserId}, NewType={UserType}",
                user.Id, request.UserType);
        }
    }
}

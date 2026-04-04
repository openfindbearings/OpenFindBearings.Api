using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 创建游客用户命令处理器
    /// </summary>
    public class CreateGuestUserCommandHandler : IRequestHandler<CreateGuestUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateGuestUserCommandHandler> _logger;

        public CreateGuestUserCommandHandler(
            IUserRepository userRepository,
            ILogger<CreateGuestUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateGuestUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建游客用户: SessionId={SessionId}", request.SessionId);

            var user = new User(request.SessionId);
            await _userRepository.AddAsync(user, cancellationToken);

            return user.Id;
        }
    }
}

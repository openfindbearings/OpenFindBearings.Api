using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Users.GetUserBySessionId
{
    public class GetUserBySessionIdQueryHandler : IRequestHandler<GetUserBySessionIdQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserBySessionIdQueryHandler> _logger;

        public GetUserBySessionIdQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserBySessionIdQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetUserBySessionIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("根据会话ID获取游客用户: SessionId={SessionId}", request.SessionId);

            var user = await _userRepository.GetByGuestSessionIdAsync(request.SessionId, cancellationToken);
            if (user == null) return null;

            return user.ToDto();
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.DTOs;
using OpenFindBearings.Application.Features.Users.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 根据用户ID获取用户查询处理器
    /// </summary>
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserByIdQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据用户ID获取用户: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return null;
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            return new UserDto
            {
                Id = user.Id,
                AuthUserId = user.AuthUserId,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                UserType = user.UserType.ToString(),
                MerchantId = user.MerchantId,
                MerchantName = user.Merchant?.Name,
                Roles = roles,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.DTOs;
using OpenFindBearings.Application.Features.Users.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 根据认证ID获取用户查询处理器
    /// </summary>
    public class GetUserByAuthIdQueryHandler : IRequestHandler<GetUserByAuthIdQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByAuthIdQueryHandler> _logger;

        public GetUserByAuthIdQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserByAuthIdQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetUserByAuthIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据认证ID获取用户: AuthUserId={AuthUserId}", request.AuthUserId);

            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
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
                // ✅ 修改：UserType 改为通过角色判断
                UserType = user.IsAdmin ? "Admin" : (user.MerchantId.HasValue ? "MerchantStaff" : "Individual"),
                MerchantId = user.MerchantId,
                MerchantName = user.Merchant?.Name,
                Roles = roles,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}

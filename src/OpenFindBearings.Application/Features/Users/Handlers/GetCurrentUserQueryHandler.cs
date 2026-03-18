using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.DTOs;
using OpenFindBearings.Application.Features.Users.Queries;
using OpenFindBearings.Domain.Interfaces;
using System.Security.Claims;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 获取当前用户信息查询处理器
    /// </summary>
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserFavoriteRepository _favoriteRepository;
        private readonly IUserFollowRepository _followRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;

        public GetCurrentUserQueryHandler(
            IUserRepository userRepository,
            IUserFavoriteRepository favoriteRepository,
            IUserFollowRepository followRepository,
            ICorrectionRequestRepository correctionRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetCurrentUserQueryHandler> logger)
        {
            _userRepository = userRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _correctionRepository = correctionRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var authUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(authUserId))
            {
                _logger.LogWarning("未找到当前用户认证ID");
                return null;
            }

            _logger.LogInformation("获取当前用户信息: AuthUserId={AuthUserId}", authUserId);

            var user = await _userRepository.GetByAuthUserIdAsync(authUserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: AuthUserId={AuthUserId}", authUserId);
                return null;
            }

            // 获取统计信息
            var favoriteCount = await _favoriteRepository.CountByUserIdAsync(user.Id, cancellationToken);
            var followCount = await _followRepository.CountByUserIdAsync(user.Id, cancellationToken);
            var corrections = await _correctionRepository.GetByUserAsync(user.Id, cancellationToken);

            // 获取角色和权限
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            return new UserDto
            {
                Id = user.Id,
                AuthUserId = user.AuthUserId,
                Nickname = user.Nickname,
                Avatar = user.Avatar,
                Email = user.Email,
                Phone = user.Phone,
                UserType = user.UserType.ToString(),
                MerchantId = user.MerchantId,
                MerchantName = user.Merchant?.Name,
                Roles = roles,
                Permissions = permissions,
                FavoriteCount = favoriteCount,
                FollowCount = followCount,
                CorrectionCount = corrections.Count,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.DTOs;
using OpenFindBearings.Application.Features.Users.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 获取当前用户详细信息查询处理器
    /// 包含收藏数、关注数、纠错数等统计信息
    /// </summary>
    public class GetUserWithStatsQueryHandler : IRequestHandler<GetUserWithStatsQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly ILogger<GetUserWithStatsQueryHandler> _logger;

        public GetUserWithStatsQueryHandler(
            IUserRepository userRepository,
            IUserBearingFavoriteRepository favoriteRepository,
            IUserMerchantFollowRepository followRepository,
            ICorrectionRequestRepository correctionRepository,
            ILogger<GetUserWithStatsQueryHandler> logger)
        {
            _userRepository = userRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _correctionRepository = correctionRepository;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetUserWithStatsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取当前用户详细信息: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: UserId={UserId}", request.UserId);
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

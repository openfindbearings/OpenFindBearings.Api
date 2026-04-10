using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Queries
{
    /// <summary>
    /// 获取当前用户详细信息查询处理器
    /// 包含收藏数、关注数、纠错数等统计信息
    /// </summary>
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly ILogger<GetUserProfileQueryHandler> _logger;

        public GetUserProfileQueryHandler(
            IUserRepository userRepository,
            IUserBearingFavoriteRepository favoriteRepository,
            IUserMerchantFollowRepository followRepository,
            ICorrectionRequestRepository correctionRepository,
            ILogger<GetUserProfileQueryHandler> logger)
        {
            _userRepository = userRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _correctionRepository = correctionRepository;
            _logger = logger;
        }

        public async Task<UserDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取当前用户详细信息: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: UserId={UserId}", request.UserId);
                return null;
            }

            var favoriteCount = await _favoriteRepository.CountByUserIdAsync(user.Id, cancellationToken);
            var followCount = await _followRepository.CountByUserIdAsync(user.Id, cancellationToken);
            var corrections = await _correctionRepository.GetByUserAsync(user.Id, cancellationToken);

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            var dto = user.ToDto(roles, permissions);
            dto.FavoriteCount = favoriteCount;
            dto.FollowCount = followCount;
            dto.CorrectionCount = corrections.Count;
            dto.UserType = user.IsGuest ? "Guest" : (user.IsAdmin ? "Admin" : (user.MerchantId.HasValue ? "MerchantStaff" : "Individual"));
            return dto;
        }
    }
}

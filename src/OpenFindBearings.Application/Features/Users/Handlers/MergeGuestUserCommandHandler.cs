using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 合并游客数据到正式账户命令处理器
    /// </summary>
    public class MergeGuestUserCommandHandler : IRequestHandler<MergeGuestUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ILogger<MergeGuestUserCommandHandler> _logger;

        public MergeGuestUserCommandHandler(
            IUserRepository userRepository,
            IUserBearingHistoryRepository historyRepository,
            IUserBearingFavoriteRepository favoriteRepository,
            IUserMerchantFollowRepository followRepository,
            ILogger<MergeGuestUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _logger = logger;
        }

        public async Task Handle(MergeGuestUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("合并游客数据: AuthUserId={AuthUserId}, GuestSessionId={GuestSessionId}",
                request.AuthUserId, request.GuestSessionId);

            // 1. 获取游客用户
            var guestUser = await _userRepository.GetByGuestSessionIdAsync(request.GuestSessionId, cancellationToken);
            if (guestUser == null || guestUser.IsMerged)
            {
                _logger.LogDebug("游客用户不存在或已合并: SessionId={SessionId}", request.GuestSessionId);
                return;
            }

            // 2. 获取或创建正式用户
            var registeredUser = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (registeredUser == null)
            {
                registeredUser = new User(request.AuthUserId, Domain.Enums.UserType.Individual);
                await _userRepository.AddAsync(registeredUser, cancellationToken);
            }

            // 3. 迁移浏览历史
            var histories = await _historyRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var history in histories)
            {
                await _historyRepository.AddOrUpdateAsync(registeredUser.Id, history.BearingId, cancellationToken);
            }
            _logger.LogDebug("迁移浏览历史: {Count}条", histories.Count);

            // 4. 迁移收藏
            var favorites = await _favoriteRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var favorite in favorites)
            {
                if (!await _favoriteRepository.ExistsAsync(registeredUser.Id, favorite.BearingId, cancellationToken))
                {
                    await _favoriteRepository.AddAsync(new UserBearingFavorite(registeredUser.Id, favorite.BearingId), cancellationToken);
                }
            }
            _logger.LogDebug("迁移收藏: {Count}条", favorites.Count);

            // 5. 迁移关注
            var follows = await _followRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var follow in follows)
            {
                if (!await _followRepository.ExistsAsync(registeredUser.Id, follow.MerchantId, cancellationToken))
                {
                    await _followRepository.AddAsync(new UserMerchantFollow(registeredUser.Id, follow.MerchantId), cancellationToken);
                }
            }
            _logger.LogDebug("迁移关注: {Count}条", follows.Count);

            // 6. 标记游客已合并
            guestUser.MarkAsMerged(registeredUser.Id);
            await _userRepository.UpdateAsync(guestUser, cancellationToken);

            _logger.LogInformation("游客数据合并完成: SessionId={SessionId}, UserId={UserId}",
                request.GuestSessionId, registeredUser.Id);
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Users.Handlers
{
    /// <summary>
    /// 迁移游客数据命令处理器
    /// </summary>
    public class MigrateGuestDataCommandHandler : IRequestHandler<MigrateGuestDataCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ILogger<MigrateGuestDataCommandHandler> _logger;

        public MigrateGuestDataCommandHandler(
            IUserRepository userRepository,
            IUserBearingHistoryRepository historyRepository,
            IUserBearingFavoriteRepository favoriteRepository,
            IUserMerchantFollowRepository followRepository,
            ILogger<MigrateGuestDataCommandHandler> logger)
        {
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _logger = logger;
        }

        public async Task Handle(MigrateGuestDataCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始迁移游客数据: SessionId={SessionId}, TargetUserId={TargetUserId}",
                request.GuestSessionId, request.TargetUserId);

            // 1. 获取游客用户
            var guestUser = await _userRepository.GetByGuestSessionIdAsync(request.GuestSessionId, cancellationToken);
            if (guestUser == null)
            {
                _logger.LogDebug("游客用户不存在: SessionId={SessionId}", request.GuestSessionId);
                return;
            }

            // 2. 如果已经迁移过，跳过
            if (guestUser.IsMerged)
            {
                _logger.LogDebug("游客数据已迁移: SessionId={SessionId}", request.GuestSessionId);
                return;
            }

            // 3. 迁移浏览历史
            var histories = await _historyRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var history in histories)
            {
                await _historyRepository.AddOrUpdateAsync(request.TargetUserId, history.BearingId, cancellationToken);
            }
            _logger.LogDebug("迁移浏览历史: {Count}条", histories.Count);

            // 4. 迁移收藏
            var favorites = await _favoriteRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var favorite in favorites)
            {
                if (!await _favoriteRepository.ExistsAsync(request.TargetUserId, favorite.BearingId, cancellationToken))
                {
                    await _favoriteRepository.AddAsync(new UserBearingFavorite(request.TargetUserId, favorite.BearingId), cancellationToken);
                }
            }
            _logger.LogDebug("迁移收藏: {Count}条", favorites.Count);

            // 5. 迁移关注
            var follows = await _followRepository.GetByUserIdAsync(guestUser.Id, 1, int.MaxValue, cancellationToken);
            foreach (var follow in follows)
            {
                if (!await _followRepository.ExistsAsync(request.TargetUserId, follow.MerchantId, cancellationToken))
                {
                    await _followRepository.AddAsync(new UserMerchantFollow(request.TargetUserId, follow.MerchantId), cancellationToken);
                }
            }
            _logger.LogDebug("迁移关注: {Count}条", follows.Count);

            // 6. 标记游客已合并
            guestUser.MarkAsMerged(request.TargetUserId);
            await _userRepository.UpdateAsync(guestUser, cancellationToken);

            _logger.LogInformation("游客数据迁移完成: SessionId={SessionId}, TargetUserId={TargetUserId}",
                request.GuestSessionId, request.TargetUserId);
        }
    }
}

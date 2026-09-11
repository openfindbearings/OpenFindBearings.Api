using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Favorites.UnfavoriteBearing
{
    /// <summary>
    /// 取消收藏轴承命令处理器
    /// </summary>
    public class UnfavoriteBearingCommandHandler : IRequestHandler<UnfavoriteBearingCommand>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly ILogger<UnfavoriteBearingCommandHandler> _logger;

        public UnfavoriteBearingCommandHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            ILogger<UnfavoriteBearingCommandHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _logger = logger;
        }

        public async Task Handle(UnfavoriteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户取消收藏轴承: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 检查是否存在收藏
            var exists = await _favoriteRepository.ExistsAsync(request.UserId, request.BearingId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("用户未收藏该轴承: UserId={UserId}, BearingId={BearingId}",
                    request.UserId, request.BearingId);
                return;
            }

            // 直接删除收藏连接表行。
            // 改动说明：原先 user.UnfavoriteBearing() + UpdateAsync(user) 只从内存集合移除，
            // 但该集合从未加载，对库零效果、删不掉行。显式走仓储 DeleteAsync 按主键删除
            await _favoriteRepository.DeleteAsync(request.UserId, request.BearingId, cancellationToken);

            _logger.LogInformation("用户取消收藏轴承成功: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);
        }
    }
}

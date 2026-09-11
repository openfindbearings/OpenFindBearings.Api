using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Favorites.FavoriteBearing
{
    /// <summary>
    /// 收藏轴承命令处理器
    /// </summary>
    public class FavoriteBearingCommandHandler : IRequestHandler<FavoriteBearingCommand, bool>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<FavoriteBearingCommandHandler> _logger;

        public FavoriteBearingCommandHandler(
            IUserBearingFavoriteRepository favoriteRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            ILogger<FavoriteBearingCommandHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(FavoriteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户收藏轴承: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 检查轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.BearingId}");
            }

            // 检查是否已收藏
            var exists = await _favoriteRepository.ExistsAsync(request.UserId, request.BearingId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("用户已收藏该轴承: UserId={UserId}, BearingId={BearingId}",
                    request.UserId, request.BearingId);
                return false;
            }

            // 获取用户实体
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.UserId}");
            }

            // 执行收藏。
            // 改动说明：原先依赖"导航集合 Add + UpdateAsync(user)"令 EF 自动 INSERT 子实体，
            // 但 BaseEntity 构造预赋 Guid.Id，Npgsql 不启用值生成器，EF 导航修复把新实体判为
            // Modified→UPDATE 不存在行→并发异常整批回滚。现显式 Add 连接表实体，
            // 收藏不修改 Users 行故移除多余 UpdateAsync，由 UnitOfWork 统一提交
            user.FavoriteBearing(request.BearingId);
            var favorite = user.FavoriteBearings.Last(f => f.BearingId == request.BearingId);
            await _favoriteRepository.AddAsync(favorite, cancellationToken);

            _logger.LogInformation("用户收藏轴承成功: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            return true;
        }
    }
}

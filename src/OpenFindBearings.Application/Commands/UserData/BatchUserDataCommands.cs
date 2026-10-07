using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.UserData
{
    /// <summary>
    /// 批量取消收藏命令（v2.12.0 列表多选）：按 bearingId 集逐条删除当前用户收藏行，
    /// 单次 UnitOfWork 提交；不存在的行静默跳过
    /// </summary>
    public record BatchRemoveFavoritesCommand : IRequest<int>, ICommand
    {
        /// <summary>轴承 ID 集</summary>
        public List<Guid> BearingIds { get; init; } = new();

        /// <summary>用户</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>批量取消收藏处理器</summary>
    public class BatchRemoveFavoritesCommandHandler : IRequestHandler<BatchRemoveFavoritesCommand, int>
    {
        private readonly IUserBearingFavoriteRepository _favoriteRepository;

        /// <summary>构造：收藏仓储</summary>
        public BatchRemoveFavoritesCommandHandler(IUserBearingFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        /// <inheritdoc/>
        public async Task<int> Handle(BatchRemoveFavoritesCommand request, CancellationToken cancellationToken)
        {
            var count = 0;
            foreach (var bearingId in request.BearingIds.Distinct())
            {
                await _favoriteRepository.DeleteAsync(request.UserId, bearingId, cancellationToken);
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// 批量取消关注命令（v2.12.0 列表多选）：按 merchantId 集逐条删除当前用户关注行，
    /// 单次 UnitOfWork 提交
    /// </summary>
    public record BatchRemoveFollowsCommand : IRequest<int>, ICommand
    {
        /// <summary>商家 ID 集</summary>
        public List<Guid> MerchantIds { get; init; } = new();

        /// <summary>用户</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>批量取消关注处理器</summary>
    public class BatchRemoveFollowsCommandHandler : IRequestHandler<BatchRemoveFollowsCommand, int>
    {
        private readonly IUserMerchantFollowRepository _followRepository;

        /// <summary>构造：关注仓储</summary>
        public BatchRemoveFollowsCommandHandler(IUserMerchantFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }

        /// <inheritdoc/>
        public async Task<int> Handle(BatchRemoveFollowsCommand request, CancellationToken cancellationToken)
        {
            var count = 0;
            foreach (var merchantId in request.MerchantIds.Distinct())
            {
                await _followRepository.DeleteAsync(request.UserId, merchantId, cancellationToken);
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// 批量删除浏览历史命令（v2.12.0 列表多选）：按目标 ID 集删除（轴承/商家两组各走一张表），
    /// 归属以 userId+目标Id 收敛（与单删同款——先查本人历史行再按行 Id 删），单次 UnitOfWork 提交
    /// </summary>
    public record BatchDeleteHistoryCommand : IRequest<int>, ICommand
    {
        /// <summary>轴承 ID 集（删轴承历史）</summary>
        public List<Guid> BearingIds { get; init; } = new();

        /// <summary>商家 ID 集（删商家历史）</summary>
        public List<Guid> MerchantIds { get; init; } = new();

        /// <summary>用户</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 批量删除历史处理器。归属守卫：先按 (userId,目标Id) 查本人历史行，命中才删行
    /// </summary>
    public class BatchDeleteHistoryCommandHandler : IRequestHandler<BatchDeleteHistoryCommand, int>
    {
        private readonly IUserBearingHistoryRepository _bearingHistoryRepository;
        private readonly IUserMerchantHistoryRepository _merchantHistoryRepository;

        /// <summary>构造：两类历史仓储</summary>
        public BatchDeleteHistoryCommandHandler(
            IUserBearingHistoryRepository bearingHistoryRepository,
            IUserMerchantHistoryRepository merchantHistoryRepository)
        {
            _bearingHistoryRepository = bearingHistoryRepository;
            _merchantHistoryRepository = merchantHistoryRepository;
        }

        /// <inheritdoc/>
        public async Task<int> Handle(BatchDeleteHistoryCommand request, CancellationToken cancellationToken)
        {
            var count = 0;
            foreach (var bearingId in request.BearingIds.Distinct())
            {
                var history = await _bearingHistoryRepository.GetAsync(request.UserId, bearingId, cancellationToken);
                if (history == null) continue;
                await _bearingHistoryRepository.DeleteAsync(history.Id, cancellationToken);
                count++;
            }
            foreach (var merchantId in request.MerchantIds.Distinct())
            {
                var history = await _merchantHistoryRepository.GetAsync(request.UserId, merchantId, cancellationToken);
                if (history == null) continue;
                await _merchantHistoryRepository.DeleteAsync(history.Id, cancellationToken);
                count++;
            }
            return count;
        }
    }
}

using MediatR;

namespace OpenFindBearings.Application.Features.Favorites.Queries
{
    /// <summary>
    /// 检查轴承是否已收藏查询
    /// </summary>
    public record CheckBearingFavoriteQuery : IRequest<bool>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; init; }
    }
}

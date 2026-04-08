using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Favorites.CheckBearingFavorite
{
    /// <summary>
    /// 检查轴承是否已收藏查询
    /// </summary>
    public record CheckBearingFavoriteQuery : IRequest<bool>, IQuery
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

using MediatR;

namespace OpenFindBearings.Application.Features.Favorites.Queries
{
    /// <summary>
    /// 检查轴承是否已收藏查询
    /// </summary>
    public record CheckBearingFavoriteQuery(
        Guid BearingId,
        string AuthUserId
    ) : IRequest<bool>;
}

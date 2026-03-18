using MediatR;
using OpenFindBearings.Application.Features.Favorites.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Favorites.Queries
{
    /// <summary>
    /// 获取我的收藏轴承列表查询
    /// </summary>
    public record GetMyFavoriteBearingsQuery(
        string AuthUserId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<FavoriteBearingDto>>;
}

using MediatR;
using OpenFindBearings.Application.Features.Favorites.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Favorites.Queries
{
    /// <summary>
    /// 获取我的收藏轴承列表查询
    /// </summary>
    public record GetMyFavoriteBearingsQuery : IRequest<PagedResult<FavoriteBearingDto>>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; init; } = 20;
    }
}

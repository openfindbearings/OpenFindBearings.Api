using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Favorites.GetMyFavoriteBearings
{
    /// <summary>
    /// 获取我的收藏轴承列表查询
    /// </summary>
    public record GetMyFavoriteBearingsQuery : IRequest<PagedResult<FavoriteBearingDto>>, IQuery
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

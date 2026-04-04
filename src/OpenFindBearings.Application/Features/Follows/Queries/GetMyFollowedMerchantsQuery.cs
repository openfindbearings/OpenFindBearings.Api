using MediatR;
using OpenFindBearings.Application.Features.Follows.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Follows.Queries
{
    /// <summary>
    /// 获取我关注的商家列表查询
    /// </summary>
    public record GetMyFollowedMerchantsQuery : IRequest<PagedResult<FollowedMerchantDto>>
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

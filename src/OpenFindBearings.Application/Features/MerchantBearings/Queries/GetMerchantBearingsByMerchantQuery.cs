using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取指定商家的所有关联查询
    /// </summary>
    public record GetMerchantBearingsByMerchantQuery : IRequest<PagedResult<MerchantBearingDto>>
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 是否只显示在售商品
        /// </summary>
        public bool? OnlyOnSale { get; init; }

        /// <summary>
        /// 当前用户是否已登录（由API层传入）
        /// </summary>
        public bool IsAuthenticated { get; init; }

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

using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 获取商家产品列表查询
    /// </summary>
    public record GetMerchantProductsQuery : IRequest<PagedResult<MerchantBearingDto>>
    {
        public Guid MerchantId { get; set; }
        public bool OnlyOnSale { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 用户是否已登录（用于价格可见性计算）
        /// </summary>
        public bool IsAuthenticated { get; set; }
    }
}

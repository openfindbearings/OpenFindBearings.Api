using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 获取商家产品列表查询
    /// </summary>
    public record GetMerchantProductsQuery(
        Guid MerchantId,
        bool OnlyOnSale = true,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<MerchantBearingDto>>;
}

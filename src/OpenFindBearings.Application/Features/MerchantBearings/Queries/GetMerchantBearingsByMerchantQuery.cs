using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取指定商家的所有关联查询
    /// </summary>
    public record GetMerchantBearingsByMerchantQuery(
        Guid MerchantId,
        bool? OnlyOnSale = null,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<MerchantBearingDto>>;
}

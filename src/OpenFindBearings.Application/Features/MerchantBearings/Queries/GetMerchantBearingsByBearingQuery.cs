using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取指定轴承的在售商家列表查询
    /// </summary>
    public record GetMerchantBearingsByBearingQuery(
        Guid BearingId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<MerchantBearingDto>>;
}

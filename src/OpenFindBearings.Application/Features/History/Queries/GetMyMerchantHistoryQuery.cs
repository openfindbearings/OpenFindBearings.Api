using MediatR;
using OpenFindBearings.Application.Features.History.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.History.Queries
{
    /// <summary>
    /// 获取我的商家浏览历史查询
    /// </summary>
    public record GetMyMerchantHistoryQuery(
        string AuthUserId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<MerchantHistoryDto>>;
}

using MediatR;
using OpenFindBearings.Application.Features.Follows.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Follows.Queries
{
    /// <summary>
    /// 获取我关注的商家列表查询
    /// </summary>
    public record GetMyFollowedMerchantsQuery(
        string AuthUserId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<FollowedMerchantDto>>;
}

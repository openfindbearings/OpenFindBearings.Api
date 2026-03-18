using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取商家纠错列表查询
    /// </summary>
    public record GetMerchantCorrectionsQuery(
        Guid MerchantId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<CorrectionDto>>;
}

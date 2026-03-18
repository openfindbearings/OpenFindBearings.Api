using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取轴承纠错列表查询
    /// </summary>
    public record GetBearingCorrectionsQuery(
        Guid BearingId,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<CorrectionDto>>;
}

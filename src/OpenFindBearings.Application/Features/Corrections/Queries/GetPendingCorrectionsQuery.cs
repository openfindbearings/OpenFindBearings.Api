using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取待审核纠错列表查询（管理员用）
    /// </summary>
    public record GetPendingCorrectionsQuery(
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedResult<CorrectionDto>>;
}

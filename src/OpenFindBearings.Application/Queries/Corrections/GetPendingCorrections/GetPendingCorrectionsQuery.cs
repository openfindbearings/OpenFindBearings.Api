using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetPendingCorrections
{
    /// <summary>
    /// 获取待审核纠错列表查询（管理员用）
    /// </summary>
    public record GetPendingCorrectionsQuery : IRequest<PagedResult<CorrectionDto>>, IQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

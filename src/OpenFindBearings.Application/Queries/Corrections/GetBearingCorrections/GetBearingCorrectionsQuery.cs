using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetBearingCorrections
{
    /// <summary>
    /// 获取轴承纠错列表查询
    /// </summary>
    public record GetBearingCorrectionsQuery : IRequest<PagedResult<CorrectionDto>>, IQuery
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; init; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; init; } = 20;
    }
}

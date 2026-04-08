using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectionDetail
{
    /// <summary>
    /// 获取单个纠错详情查询
    /// </summary>
    public record GetCorrectionDetailQuery : IRequest<CorrectionDto?>, IQuery
    {
        /// <summary>
        /// 纠错ID
        /// </summary>
        public Guid Id { get; init; }
    }
}

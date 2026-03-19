using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取单个纠错详情查询
    /// </summary>
    public record GetCorrectionDetailQuery : IRequest<CorrectionDto?>
    {
        /// <summary>
        /// 纠错ID
        /// </summary>
        public Guid Id { get; init; }
    }
}

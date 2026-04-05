using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取我的单条纠错详情查询
    /// </summary>
    public record GetMyCorrectionDetailQuery : IRequest<CorrectionDto?>
    {
        /// <summary>
        /// 纠错ID
        /// </summary>
        public Guid CorrectionId { get; init; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}

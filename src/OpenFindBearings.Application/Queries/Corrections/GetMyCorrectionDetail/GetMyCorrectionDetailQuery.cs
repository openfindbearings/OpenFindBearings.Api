using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Corrections.GetMyCorrectionDetail
{
    /// <summary>
    /// 获取我的单条纠错详情查询
    /// </summary>
    public record GetMyCorrectionDetailQuery : IRequest<CorrectionDto?>, IQuery
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

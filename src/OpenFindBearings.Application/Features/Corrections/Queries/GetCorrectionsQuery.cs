using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取纠错列表查询（管理员用）
    /// </summary>
    public record GetCorrectionsQuery : IRequest<PagedResult<CorrectionDto>>
    {
        /// <summary>
        /// 目标类型（Bearing/Merchant）
        /// </summary>
        public string? TargetType { get; init; }

        /// <summary>
        /// 目标ID
        /// </summary>
        public Guid? TargetId { get; init; }

        /// <summary>
        /// 状态（Pending/Approved/Rejected）
        /// </summary>
        public string? Status { get; init; }

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

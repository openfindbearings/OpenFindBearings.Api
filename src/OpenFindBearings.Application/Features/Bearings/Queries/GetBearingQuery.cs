using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 获取单个轴承查询
    /// </summary>
    public record GetBearingQuery : IRequest<BearingDetailDto?>
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 用户ID（用于记录浏览历史）
        /// </summary>
        public Guid? UserId { get; init; }

        /// <summary>
        /// 会话ID（游客记录浏览历史）
        /// </summary>
        public string? SessionId { get; init; }
    }
}

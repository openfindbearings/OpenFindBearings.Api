using MediatR;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.Corrections.Queries
{
    /// <summary>
    /// 获取我提交的纠错列表查询
    /// </summary>
    public record GetMyCorrectionsQuery : IRequest<PagedResult<CorrectionDto>>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

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

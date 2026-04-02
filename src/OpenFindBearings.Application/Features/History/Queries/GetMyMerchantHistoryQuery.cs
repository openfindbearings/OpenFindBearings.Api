using MediatR;
using OpenFindBearings.Application.Features.History.DTOs;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Queries
{
    /// <summary>
    /// 获取我的商家浏览历史查询
    /// </summary>
    public record GetMyMerchantHistoryQuery : IRequest<PagedResult<MerchantHistoryDto>>
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

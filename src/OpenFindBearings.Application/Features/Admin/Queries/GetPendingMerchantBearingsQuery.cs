using MediatR;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.Admin.Queries
{
    /// <summary>
    /// 获取待审核商家产品查询
    /// </summary>
    public record GetPendingMerchantBearingsQuery : IRequest<PagedResult<PendingMerchantBearingDto>>
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

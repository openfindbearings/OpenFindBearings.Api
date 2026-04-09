using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetPendingMerchantBearings
{
    /// <summary>
    /// 获取待审核商家产品查询
    /// </summary>
    public record GetPendingMerchantBearingsQuery : IRequest<PagedResult<PendingMerchantBearingDto>>, IQuery
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

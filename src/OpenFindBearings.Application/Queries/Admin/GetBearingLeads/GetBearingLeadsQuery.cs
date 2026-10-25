using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetBearingLeads
{
    /// <summary>
    /// 获取型号线索列表查询（管理端）
    /// 支持按状态与型号关键词筛选，状态为计算态（已忽略 / 已入库 / 待建档）
    /// </summary>
    public class GetBearingLeadsQuery : IRequest<PagedResult<BearingLeadDto>>, IQuery
    {
        /// <summary>
        /// 状态筛选（空=全部）
        /// </summary>
        public BearingLeadStatus? Status { get; set; }

        /// <summary>
        /// 型号关键词
        /// </summary>
        public string? Keyword { get; set; }

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

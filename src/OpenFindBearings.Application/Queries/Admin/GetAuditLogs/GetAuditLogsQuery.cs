using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Admin.GetAuditLogs
{
    /// <summary>
    /// 获取审核日志查询
    /// </summary>
    public class GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>, IQuery
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndDate { get; set; }

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

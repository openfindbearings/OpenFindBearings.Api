using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Domain.Parameters
{
    /// <summary>
    /// 审核日志搜索参数
    /// </summary>
    public class AuditLogSearchParams
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
        /// 实体ID
        /// </summary>
        public Guid? EntityId { get; set; }

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

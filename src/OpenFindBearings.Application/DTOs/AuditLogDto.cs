namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 审核日志DTO
    /// </summary>
    public class AuditLogDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 操作类型（Approve/Reject/Create/Update/Delete）
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// 实体ID
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// 实体名称/标识
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperatedAt { get; set; }

        /// <summary>
        /// 操作详情（JSON格式）
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// HTTP 请求方法
        /// </summary>
        public string? HttpMethod { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// HTTP 响应状态码
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// 请求处理耗时（毫秒）
        /// </summary>
        public long? DurationMs { get; set; }
    }
}

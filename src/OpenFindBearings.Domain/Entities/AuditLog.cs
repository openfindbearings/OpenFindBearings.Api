namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 审核日志实体
    /// 记录所有管理员操作
    /// </summary>
    public class AuditLog : BaseEntity
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string Action { get; private set; } = string.Empty;

        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; private set; } = string.Empty;

        /// <summary>
        /// 实体ID
        /// </summary>
        public Guid EntityId { get; private set; }

        /// <summary>
        /// 操作前数据（JSON）
        /// </summary>
        public string? BeforeData { get; private set; }

        /// <summary>
        /// 操作后数据（JSON）
        /// </summary>
        public string? AfterData { get; private set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; private set; }

        /// <summary>
        /// 操作人导航属性
        /// </summary>
        public User? Operator { get; private set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperatedAt { get; private set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; private set; }

        private AuditLog() { }

        public AuditLog(
            string action,
            string entityType,
            Guid entityId,
            Guid operatorId,
            string? beforeData = null,
            string? afterData = null,
            string? remarks = null)
        {
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            OperatorId = operatorId;
            BeforeData = beforeData;
            AfterData = afterData;
            Remarks = remarks;
            OperatedAt = DateTime.UtcNow;
        }
    }
}

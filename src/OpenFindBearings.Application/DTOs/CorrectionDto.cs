namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 纠错记录DTO
    /// 用于展示纠错列表和详情
    /// </summary>
    public class CorrectionDto
    {
        /// <summary>
        /// 纠错ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 目标类型（Bearing / Merchant）
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// 目标ID
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// 目标显示名称（型号/商家名）
        /// </summary>
        public string TargetDisplay { get; set; } = string.Empty;

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 字段显示名称（如"内径"、"电话"等）
        /// </summary>
        public string FieldDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 原始值
        /// </summary>
        public string? OriginalValue { get; set; }

        /// <summary>
        /// 建议值
        /// </summary>
        public string SuggestedValue { get; set; } = string.Empty;

        /// <summary>
        /// 纠错理由
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 提交人ID
        /// </summary>
        public Guid SubmittedBy { get; set; }

        /// <summary>
        /// 提交人姓名
        /// </summary>
        public string SubmitterName { get; set; } = string.Empty;

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 审核人ID
        /// </summary>
        public Guid? ReviewedBy { get; set; }

        /// <summary>
        /// 审核人姓名
        /// </summary>
        public string? ReviewerName { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// 审核意见
        /// </summary>
        public string? ReviewComment { get; set; }
    }
}

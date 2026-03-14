using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 纠错请求实体
    /// 用户发现产品信息或商家信息有误时提交的纠错申请
    /// 需要管理员审核后才能生效
    /// </summary>
    public class CorrectionRequest : BaseEntity
    {
        /// <summary>
        /// 实体类型
        /// 指定要纠错的实体类型，如 "Bearing"（产品）、"Merchant"（商家）
        /// </summary>
        public string EntityType { get; private set; } = string.Empty;

        /// <summary>
        /// 实体ID
        /// 要纠错的具体实体在数据库中的唯一标识
        /// </summary>
        public Guid EntityId { get; private set; }

        /// <summary>
        /// 字段名称
        /// 要修改的字段名，如 "PartNumber"、"Name"、"Phone" 等
        /// </summary>
        public string FieldName { get; private set; } = string.Empty;

        /// <summary>
        /// 原始值
        /// 当前系统中存储的旧值，用于对比和记录
        /// </summary>
        public string? OriginalValue { get; private set; }

        /// <summary>
        /// 建议值
        /// 用户提交的新值，期望修改为这个值
        /// </summary>
        public string SuggestedValue { get; private set; } = string.Empty;

        /// <summary>
        /// 纠错理由
        /// 用户说明为什么要修改，可选项
        /// </summary>
        public string? Reason { get; private set; }

        /// <summary>
        /// 提交人ID
        /// 发起纠错请求的用户ID
        /// </summary>
        public Guid SubmittedBy { get; private set; }

        /// <summary>
        /// 提交时间（UTC）
        /// </summary>
        public DateTime SubmittedAt { get; private set; }

        /// <summary>
        /// 状态
        /// Pending: 待审核
        /// Approved: 已通过（系统会自动更新对应字段）
        /// Rejected: 已拒绝
        /// </summary>
        public CorrectionStatus Status { get; private set; }

        /// <summary>
        /// 审核人ID
        /// 处理此纠错请求的管理员ID
        /// </summary>
        public Guid? ReviewedBy { get; private set; }

        /// <summary>
        /// 审核时间（UTC）
        /// </summary>
        public DateTime? ReviewedAt { get; private set; }

        /// <summary>
        /// 审核意见
        /// 管理员审核时填写的备注或拒绝理由
        /// </summary>
        public string? ReviewComment { get; private set; }

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private CorrectionRequest() { }

        /// <summary>
        /// 创建纠错请求
        /// </summary>
        /// <param name="entityType">实体类型（Bearing/Merchant）</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="suggestedValue">建议值</param>
        /// <param name="submittedBy">提交人ID</param>
        /// <param name="originalValue">原始值（可选）</param>
        /// <param name="reason">纠错理由（可选）</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public CorrectionRequest(
            string entityType,
            Guid entityId,
            string fieldName,
            string suggestedValue,
            Guid submittedBy,
            string? originalValue = null,
            string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("实体类型不能为空", nameof(entityType));
            if (entityId == Guid.Empty)
                throw new ArgumentException("实体ID不能为空", nameof(entityId));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("字段名称不能为空", nameof(fieldName));
            if (string.IsNullOrWhiteSpace(suggestedValue))
                throw new ArgumentException("建议值不能为空", nameof(suggestedValue));
            if (submittedBy == Guid.Empty)
                throw new ArgumentException("提交人ID不能为空", nameof(submittedBy));

            Id = Guid.NewGuid();
            EntityType = entityType;
            EntityId = entityId;
            FieldName = fieldName;
            OriginalValue = originalValue;
            SuggestedValue = suggestedValue;
            Reason = reason;
            SubmittedBy = submittedBy;
            SubmittedAt = DateTime.UtcNow;
            Status = CorrectionStatus.Pending;
        }

        /// <summary>
        /// 审核通过
        /// 管理员确认纠错有效，系统将执行更新操作
        /// </summary>
        /// <param name="reviewedBy">审核人ID</param>
        /// <param name="comment">审核意见（可选）</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public void Approve(Guid reviewedBy, string? comment = null)
        {
            if (reviewedBy == Guid.Empty)
                throw new ArgumentException("审核人ID不能为空", nameof(reviewedBy));
            if (Status != CorrectionStatus.Pending)
                throw new InvalidOperationException($"只能审核状态为 Pending 的请求，当前状态为 {Status}");

            Status = CorrectionStatus.Approved;
            ReviewedBy = reviewedBy;
            ReviewedAt = DateTime.UtcNow;
            ReviewComment = comment;
            UpdateTimestamp();
        }

        /// <summary>
        /// 审核拒绝
        /// 管理员认为纠错无效，拒绝修改
        /// </summary>
        /// <param name="reviewedBy">审核人ID</param>
        /// <param name="comment">拒绝理由（必填）</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public void Reject(Guid reviewedBy, string comment)
        {
            if (reviewedBy == Guid.Empty)
                throw new ArgumentException("审核人ID不能为空", nameof(reviewedBy));
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("拒绝时必须填写审核意见", nameof(comment));
            if (Status != CorrectionStatus.Pending)
                throw new InvalidOperationException($"只能审核状态为 Pending 的请求，当前状态为 {Status}");

            Status = CorrectionStatus.Rejected;
            ReviewedBy = reviewedBy;
            ReviewedAt = DateTime.UtcNow;
            ReviewComment = comment;
            UpdateTimestamp();
        }

        /// <summary>
        /// 判断当前请求是否已处理（通过或拒绝）
        /// </summary>
        public bool IsProcessed => Status != CorrectionStatus.Pending;

        /// <summary>
        /// 获取实体类型的显示名称
        /// </summary>
        public string GetEntityTypeDisplayName()
        {
            return EntityType switch
            {
                "Bearing" => "轴承产品",
                "Merchant" => "商家信息",
                _ => EntityType
            };
        }

        /// <summary>
        /// 获取字段的显示名称
        /// 可以根据实际业务需求扩展
        /// </summary>
        public string GetFieldDisplayName()
        {
            // 可以根据 EntityType 和 FieldName 返回友好的字段名
            if (EntityType == "Bearing")
            {
                return FieldName switch
                {
                    "PartNumber" => "型号",
                    "Name" => "名称",
                    "Description" => "描述",
                    "InnerDiameter" => "内径",
                    "OuterDiameter" => "外径",
                    "Width" => "宽度",
                    _ => FieldName
                };
            }
            else if (EntityType == "Merchant")
            {
                return FieldName switch
                {
                    "Name" => "商家名称",
                    "CompanyName" => "公司全称",
                    "Phone" => "电话",
                    "Email" => "邮箱",
                    "Address" => "地址",
                    _ => FieldName
                };
            }
            return FieldName;
        }
    }
}

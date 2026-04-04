using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 纠错请求实体
    /// 用户发现产品信息或商家信息有误时提交的纠错申请
    /// 需要管理员审核后才能生效
    /// 对应接口：POST /api/bearings/{id}/corrections、POST /api/merchants/{id}/corrections
    /// </summary>
    public class CorrectionRequest : BaseEntity
    {
        /// <summary>
        /// 纠错目标类型
        /// Bearing: 轴承纠错
        /// Merchant: 商家纠错
        /// </summary>
        public string TargetType { get; private set; } = string.Empty;

        /// <summary>
        /// 目标ID（可以是轴承ID或商家ID）
        /// </summary>
        public Guid TargetId { get; private set; }

        /// <summary>
        /// 轴承导航属性（当TargetType为Bearing时）
        /// </summary>
        public Bearing? Bearing { get; private set; }

        /// <summary>
        /// 商家导航属性（当TargetType为Merchant时）
        /// </summary>
        public Merchant? Merchant { get; private set; }

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
        /// 提交人导航属性
        /// </summary>
        public User? Submitter { get; private set; }

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
        /// 审核人导航属性
        /// </summary>
        public User? Reviewer { get; private set; }

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
        /// <param name="targetType">目标类型 "Bearing" 或 "Merchant"</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="suggestedValue">建议值</param>
        /// <param name="submittedBy">提交人ID</param>
        /// <param name="originalValue">原始值（可选）</param>
        /// <param name="reason">纠错理由（可选）</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public CorrectionRequest(
            string targetType,
            Guid targetId,
            string fieldName,
            string suggestedValue,
            Guid submittedBy,
            string? originalValue = null,
            string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                throw new ArgumentException("目标类型不能为空", nameof(targetType));
            if (targetType != "Bearing" && targetType != "Merchant")
                throw new ArgumentException("目标类型必须是 Bearing 或 Merchant", nameof(targetType));
            if (targetId == Guid.Empty)
                throw new ArgumentException("目标ID不能为空", nameof(targetId));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("字段名称不能为空", nameof(fieldName));
            if (string.IsNullOrWhiteSpace(suggestedValue))
                throw new ArgumentException("建议值不能为空", nameof(suggestedValue));
            if (submittedBy == Guid.Empty)
                throw new ArgumentException("提交人ID不能为空", nameof(submittedBy));

            TargetType = targetType;
            TargetId = targetId;
            FieldName = fieldName;
            SuggestedValue = suggestedValue;
            OriginalValue = originalValue;
            Reason = reason;
            SubmittedBy = submittedBy;
            SubmittedAt = DateTime.UtcNow;
            Status = CorrectionStatus.Pending;
        }

        /// <summary>
        /// 创建轴承纠错（便捷方法）
        /// </summary>
        public static CorrectionRequest ForBearing(
            Guid bearingId,
            string fieldName,
            string suggestedValue,
            Guid submittedBy,
            string? originalValue = null,
            string? reason = null)
        {
            return new CorrectionRequest("Bearing", bearingId, fieldName, suggestedValue,
                submittedBy, originalValue, reason);
        }

        /// <summary>
        /// 创建商家纠错（便捷方法）
        /// </summary>
        public static CorrectionRequest ForMerchant(
            Guid merchantId,
            string fieldName,
            string suggestedValue,
            Guid submittedBy,
            string? originalValue = null,
            string? reason = null)
        {
            return new CorrectionRequest("Merchant", merchantId, fieldName, suggestedValue,
                submittedBy, originalValue, reason);
        }

        /// <summary>
        /// 审核通过
        /// </summary>
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
        /// </summary>
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
        /// 判断当前请求是否已处理
        /// </summary>
        public bool IsProcessed => Status != CorrectionStatus.Pending;

        /// <summary>
        /// 获取实体类型的显示名称
        /// </summary>
        public string GetEntityTypeDisplayName()
        {
            return TargetType switch
            {
                "Bearing" => "轴承产品",
                "Merchant" => "商家信息",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取字段的显示名称
        /// </summary>
        public string GetFieldDisplayName()
        {
            if (TargetType == "Bearing")
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
            else if (TargetType == "Merchant")
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

using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商户证照材料审核记录（v2.7.0 由 LicenseVerification 泛化改名）。
    /// 承载营业执照/品牌授权书/厂房照片等多类材料：随入驻申请单一起提交、在审批抽屉一次审，
    /// 入驻后的换证/补材料走"商户文档审核"独立队列。
    /// </summary>
    public class MerchantDocument : BaseEntity
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; private set; }

        /// <summary>
        /// 商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 材料类型（营业执照/品牌授权书/厂房照片）
        /// </summary>
        public DocumentType Type { get; private set; }

        /// <summary>
        /// 材料文件URL（媒体服务相对键）
        /// </summary>
        public string FileUrl { get; private set; } = string.Empty;

        /// <summary>
        /// 审核状态
        /// </summary>
        public DocumentStatus Status { get; private set; }

        /// <summary>
        /// 提交人ID
        /// </summary>
        public Guid SubmittedBy { get; private set; }

        /// <summary>
        /// 提交人导航属性
        /// </summary>
        public User? Submitter { get; private set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime SubmittedAt { get; private set; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public Guid? ReviewedBy { get; private set; }

        /// <summary>
        /// 审核人导航属性
        /// </summary>
        public User? Reviewer { get; private set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ReviewedAt { get; private set; }

        /// <summary>
        /// 审核意见（拒绝原因等）
        /// </summary>
        public string? ReviewComment { get; private set; }

        private MerchantDocument() { }

        /// <summary>
        /// 新建待审材料记录（提交即 Pending，等待平台审核）
        /// </summary>
        public MerchantDocument(
            Guid merchantId,
            DocumentType type,
            string fileUrl,
            Guid submittedBy)
        {
            MerchantId = merchantId;
            Type = type;
            FileUrl = fileUrl;
            SubmittedBy = submittedBy;
            Status = DocumentStatus.Pending;
            SubmittedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 审核通过
        /// </summary>
        public void Approve(Guid reviewerId, string? comment = null)
        {
            Status = DocumentStatus.Approved;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            ReviewComment = comment;
            UpdateTimestamp();
        }

        /// <summary>
        /// 审核拒绝
        /// </summary>
        public void Reject(Guid reviewerId, string reason)
        {
            Status = DocumentStatus.Rejected;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            ReviewComment = reason;
            UpdateTimestamp();
        }
    }
}

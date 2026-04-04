using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 营业执照审核记录
    /// </summary>
    public class LicenseVerification : BaseEntity
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
        /// 营业执照文件URL
        /// </summary>
        public string LicenseUrl { get; private set; } = string.Empty;

        /// <summary>
        /// 审核状态
        /// </summary>
        public LicenseVerificationStatus Status { get; private set; }

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

        private LicenseVerification() { }

        public LicenseVerification(
            Guid merchantId,
            string licenseUrl,
            Guid submittedBy)
        {
            MerchantId = merchantId;
            LicenseUrl = licenseUrl;
            SubmittedBy = submittedBy;
            Status = LicenseVerificationStatus.Pending;
            SubmittedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 审核通过
        /// </summary>
        public void Approve(Guid reviewerId, string? comment = null)
        {
            Status = LicenseVerificationStatus.Approved;
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
            Status = LicenseVerificationStatus.Rejected;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            ReviewComment = reason;
            UpdateTimestamp();
        }
    }
}

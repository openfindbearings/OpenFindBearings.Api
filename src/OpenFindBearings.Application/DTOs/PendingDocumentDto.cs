namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 待审/已审证照材料队列项（v2.7.0 由 PendingLicenseDto 泛化改名，Admin"商户文档审核"队列与审批抽屉共用）
    /// </summary>
    public class PendingDocumentDto
    {
        /// <summary>材料记录ID</summary>
        public Guid Id { get; set; }

        /// <summary>商家ID</summary>
        public Guid MerchantId { get; set; }

        /// <summary>商家名称</summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>材料类型枚举值（1 执照 / 2 授权书 / 3 厂房照）</summary>
        public int Type { get; set; }

        /// <summary>材料类型中文名</summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>材料文件URL</summary>
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>审核状态（Pending/Approved/Rejected）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>提交人ID</summary>
        public Guid SubmittedBy { get; set; }

        /// <summary>提交人昵称</summary>
        public string SubmitterName { get; set; } = string.Empty;

        /// <summary>提交时间（UTC）</summary>
        public DateTime SubmittedAt { get; set; }
    }
}

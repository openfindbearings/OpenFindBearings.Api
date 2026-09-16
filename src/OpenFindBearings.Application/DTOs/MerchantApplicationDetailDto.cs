namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 入驻申请详情DTO（v2.6.0 新增）——被拒重提的表单预填数据源，
    /// 比列表项 MerchantApplicationDto 多出可编辑的全量资料字段。
    /// </summary>
    public class MerchantApplicationDetailDto
    {
        /// <summary>商户ID</summary>
        public Guid MerchantId { get; set; }

        /// <summary>商户名称</summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>商户状态（Pending/Active/Suspended）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>驳回原因（Suspended 时有值）</summary>
        public string? RejectReason { get; set; }

        /// <summary>入驻渠道（self/claim/nomination/none，前端据此决定编辑页可改范围）</summary>
        public string ApplicationMode { get; set; } = string.Empty;

        /// <summary>当前用户在该商户的角色</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>商家类型枚举值（编辑表单回填）</summary>
        public int Type { get; set; }

        /// <summary>营业执照企业名称</summary>
        public string? CompanyName { get; set; }

        /// <summary>统一社会信用代码</summary>
        public string? UnifiedSocialCreditCode { get; set; }

        /// <summary>对外联系人</summary>
        public string? ContactPerson { get; set; }

        /// <summary>客服电话（对外公开）</summary>
        public string? Phone { get; set; }

        /// <summary>手机</summary>
        public string? Mobile { get; set; }

        /// <summary>邮箱</summary>
        public string? Email { get; set; }

        /// <summary>地址</summary>
        public string? Address { get; set; }

        /// <summary>商户简介</summary>
        public string? Description { get; set; }

        /// <summary>Logo 相对媒体键</summary>
        public string? LogoUrl { get; set; }
    }
}

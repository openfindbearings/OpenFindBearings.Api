namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商户入驻状态DTO
    /// 展示用户在某商户的入驻进度（待审核/已生效/已拒绝）
    /// </summary>
    public class MerchantApplicationDto
    {
        /// <summary>
        /// 商户ID
        /// </summary>
        public Guid MerchantId { get; set; }

        /// <summary>
        /// 商户名称
        /// </summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>
        /// 商户状态（Pending 待审核 / Active 已生效 / Suspended 已拒绝）
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 拒绝原因（已拒绝时有值）
        /// </summary>
        public string? RejectReason { get; set; }

        /// <summary>
        /// 用户在该商户的角色（MerchantAdmin / MerchantStaff）
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 是否已认证（资质等级）
        /// </summary>
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// 可认领爬虫商家DTO（入驻认领搜索项）
    /// </summary>
    public class ClaimableMerchantDto
    {
        /// <summary>
        /// 商户ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 商户名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 公司全称
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public string Type { get; set; } = string.Empty;
    }
}

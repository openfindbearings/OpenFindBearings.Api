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

    /// <summary>
    /// 商户是否已主动申请认证（v2.9.0：Taro"申请认证"按钮态——已申请显示"已提交"禁用）
    /// </summary>
    public bool VerifyRequested { get; set; }

        /// <summary>
        /// 商户 Logo 相对/绝对 URL（供移动端商户切换器与 TabBar 展示当前商户头像）
        /// </summary>
        public string? LogoUrl { get; set; }
    }

    /// <summary>
    /// 入驻搜索项DTO（v2.9.0 由"仅可认领池"升级为"全量发现"：搜索结果含已入驻商户，
    /// 带认领可行性标记，让用户在搜索阶段就避免重复新建，而非提交时撞 409）
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

        /// <summary>
        /// 是否可认领（未认证 + 无在职成员 + 无进行中提名锁定）
        /// </summary>
        public bool IsClaimable { get; set; }

        /// <summary>
        /// 当前登录用户是否已是该商户在职成员（"去管理"入口）
        /// </summary>
        public bool IsMine { get; set; }

        /// <summary>
        /// 状态文案：可认领 / 我的商户 / 已入驻 / 审核中 / 已认证
        /// </summary>
        public string StatusText { get; set; } = string.Empty;
    }
}

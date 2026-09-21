namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商家详情DTO
    /// 用于商家详情页展示
    /// </summary>
    public class MerchantDetailDto : MerchantDto
    {
        /// <summary>
        /// 商家简介
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 经营范围
        /// </summary>
        public string? BusinessScope { get; set; }

        /// <summary>
        /// 认证时间
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// 官网（商户信息维护可编辑）
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// 统一社会信用代码（商户信息维护可编辑）
        /// </summary>
        public string? UnifiedSocialCreditCode { get; set; }

        /// <summary>
        /// 在售产品列表
        /// </summary>
        public List<MerchantBearingDto> Products { get; set; } = new();

        /// <summary>
        /// 是否已关注（针对当前登录用户）
        /// </summary>
        public bool IsFollowed { get; set; }
    }

    /// <summary>
    /// 商家员工DTO
    /// </summary>
    public class MerchantStaffDto
    {
        /// <summary>
        /// 员工ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 员工昵称
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// 员工头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 手机号（v2.11.0 员工详情用：同商户成员互见联系方式是协作刚需，钉钉同款；
        ///   仅本商户成员列表端点返回，不对公开接口暴露）
        /// </summary>
        public string? Mobile { get; set; }

        /// <summary>
        /// 加入时间（成员行创建时间，v2.11.0 详情面板展示；邀请行为 null）
        /// </summary>
        public DateTime? JoinedAt { get; set; }

        /// <summary>
        /// 在商家的角色
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 成员状态（Active / Suspended，成员管理展示用）
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 是否为当前登录用户本人（v1.5.2：成员管理页据此隐藏自操作按钮，前端 id 不同源无法自判）
        /// </summary>
        public bool IsSelf { get; set; }

        /// <summary>
        /// 邀请ID（v2.9.0 邀请确认制：Status=Invited 的行为待确认邀请而非成员，撤销操作用此ID）
        /// </summary>
        public Guid? InvitationId { get; set; }
    }
}

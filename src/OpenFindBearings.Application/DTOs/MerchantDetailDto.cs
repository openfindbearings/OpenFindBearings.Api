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
        /// 员工列表
        /// </summary>
        public List<MerchantStaffDto> Staff { get; set; } = new();

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
        /// 在商家的角色
        /// </summary>
        public string? Role { get; set; }
    }
}

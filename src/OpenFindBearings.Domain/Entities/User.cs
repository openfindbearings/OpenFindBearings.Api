using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    public class User : BaseEntity
    {
        public string AuthUserId { get; set; } = string.Empty;  // 认证系统用户ID（登录用户才有）
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        public UserType UserType { get; set; }

        // 个人用户的额外信息（可选）
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // 游客会话ID（未登录时用）
        public string? GuestSessionId { get; set; }

        public DateTime? LastLoginAt { get; set; }


        // 导航属性

        ///// <summary>
        ///// 所属商家Id
        ///// </summary>
        //public Guid? MerchantId { get; set; }
        ///// <summary>
        ///// 所属商家
        ///// </summary>
        //public Merchant? Merchant { get; set; }
    }
}

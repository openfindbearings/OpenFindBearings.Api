using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户实体
    /// 业务系统中的用户，与认证系统的用户通过 AuthUserId 关联
    /// </summary>
    public class User : BaseEntity
    {
        /// <summary>
        /// 认证系统用户ID
        /// 来自独立认证服务（Auth Service）的用户唯一标识
        /// </summary>
        public string AuthUserId { get; private set; } = string.Empty;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; private set; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string? Avatar { get; private set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        public string? Email { get; private set; }

        /// <summary>
        /// 用户类型
        /// Admin: 平台管理员
        /// MerchantStaff: 商家员工
        /// Individual: 个人用户
        /// Guest: 游客（未登录）
        /// </summary>
        public UserType UserType { get; private set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? Phone { get; private set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; private set; }

        /// <summary>
        /// 游客会话ID
        /// 未登录用户用于临时存储数据的标识
        /// </summary>
        public string? GuestSessionId { get; private set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        /// <summary>
        /// 所属商家ID
        /// 当 UserType 为 MerchantStaff 时，指向对应的商家
        /// </summary>
        public Guid? MerchantId { get; private set; }

        /// <summary>
        /// 所属商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 用户-角色关联导航属性
        /// 一个用户可以拥有多个角色
        /// </summary>
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private User() { }

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="authUserId">认证系统用户ID</param>
        /// <param name="userType">用户类型</param>
        /// <param name="nickname">昵称</param>
        /// <param name="email">邮箱</param>
        public User(
            string authUserId,
            UserType userType,
            string? nickname = null,
            string? email = null)
        {
            if (string.IsNullOrWhiteSpace(authUserId))
                throw new ArgumentException("认证用户ID不能为空", nameof(authUserId));

            AuthUserId = authUserId;
            UserType = userType;
            Nickname = nickname;
            Email = email;
        }

        /// <summary>
        /// 创建游客用户
        /// </summary>
        /// <param name="guestSessionId">游客会话ID</param>
        public User(string guestSessionId)
        {
            if (string.IsNullOrWhiteSpace(guestSessionId))
                throw new ArgumentException("游客会话ID不能为空", nameof(guestSessionId));

            GuestSessionId = guestSessionId;
            UserType = UserType.Guest;
        }

        /// <summary>
        /// 更新用户基本信息
        /// </summary>
        /// <param name="nickname">昵称</param>
        /// <param name="avatar">头像</param>
        /// <param name="phone">电话</param>
        /// <param name="address">地址</param>
        public void UpdateProfile(
            string? nickname,
            string? avatar,
            string? phone,
            string? address)
        {
            Nickname = nickname;
            Avatar = avatar;
            Phone = phone;
            Address = address;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新最后登录时间
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 关联到商家
        /// </summary>
        /// <param name="merchantId">商家ID</param>
        public void AssignToMerchant(Guid merchantId)
        {
            MerchantId = merchantId;
            UserType = UserType.MerchantStaff;
            UpdateTimestamp();
        }

        /// <summary>
        /// 从商家移除
        /// </summary>
        public void RemoveFromMerchant()
        {
            MerchantId = null;
            UserType = UserType.Individual;
            UpdateTimestamp();
        }

        /// <summary>
        /// 转换为正式用户（从游客升级）
        /// </summary>
        /// <param name="authUserId">认证系统用户ID</param>
        /// <param name="email">邮箱</param>
        public void ConvertToRegisteredUser(string authUserId, string email)
        {
            AuthUserId = authUserId;
            Email = email;
            UserType = UserType.Individual;
            GuestSessionId = null;
            UpdateTimestamp();
        }

        /// <summary>
        /// 判断是否为商家员工
        /// </summary>
        public bool IsMerchantStaff => UserType == UserType.MerchantStaff && MerchantId.HasValue;

        /// <summary>
        /// 判断是否为平台管理员
        /// </summary>
        public bool IsAdmin => UserType == UserType.Admin;

        /// <summary>
        /// 判断是否为游客
        /// </summary>
        public bool IsGuest => UserType == UserType.Guest;
    }
}

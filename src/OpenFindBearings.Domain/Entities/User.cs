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
        /// </summary>
        public string? GuestSessionId { get; private set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        /// <summary>
        /// 所属商家ID
        /// </summary>
        public Guid? MerchantId { get; private set; }

        /// <summary>
        /// 所属商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        // ============ 导航属性 ============

        /// <summary>
        /// 用户-角色关联导航属性
        /// </summary>
        public List<UserRole> UserRoles { get; private set; } = [];

        // ============ 新增：收藏与历史导航属性 ============

        /// <summary>
        /// 收藏的轴承
        /// </summary>
        public List<UserFavorite> FavoriteBearings { get; private set; } = [];

        /// <summary>
        /// 关注的商家
        /// </summary>
        public List<UserFollow> FollowedMerchants { get; private set; } = [];

        /// <summary>
        /// 轴承浏览历史
        /// </summary>
        public List<UserBearingHistory> BearingHistory { get; private set; } = [];

        /// <summary>
        /// 商家浏览历史
        /// </summary>
        public List<UserMerchantHistory> MerchantHistory { get; private set; } = [];

        /// <summary>
        /// 提交的纠错
        /// </summary>
        public List<CorrectionRequest> SubmittedCorrections { get; private set; } = [];

        // ============ 统计属性 ============

        /// <summary>
        /// 收藏数量
        /// </summary>
        public int FavoriteCount => FavoriteBearings.Count;

        /// <summary>
        /// 关注数量
        /// </summary>
        public int FollowCount => FollowedMerchants.Count;

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private User() { }

        /// <summary>
        /// 创建新用户
        /// </summary>
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
        /// 转换为正式用户
        /// </summary>
        public void ConvertToRegisteredUser(string authUserId, string email)
        {
            AuthUserId = authUserId;
            Email = email;
            UserType = UserType.Individual;
            GuestSessionId = null;
            UpdateTimestamp();
        }

        // ============ 收藏相关方法 ============

        /// <summary>
        /// 收藏轴承
        /// </summary>
        public void FavoriteBearing(Guid bearingId)
        {
            if (FavoriteBearings.Any(f => f.BearingId == bearingId))
                throw new InvalidOperationException("已经收藏过该轴承");

            var favorite = new UserFavorite(Id, bearingId);
            FavoriteBearings.Add(favorite);
            UpdateTimestamp();
        }

        /// <summary>
        /// 取消收藏轴承
        /// </summary>
        public void UnfavoriteBearing(Guid bearingId)
        {
            var favorite = FavoriteBearings.FirstOrDefault(f => f.BearingId == bearingId);
            if (favorite != null)
            {
                FavoriteBearings.Remove(favorite);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 检查是否已收藏轴承
        /// </summary>
        public bool HasFavoritedBearing(Guid bearingId)
            => FavoriteBearings.Any(f => f.BearingId == bearingId);

        // ============ 关注相关方法 ============

        /// <summary>
        /// 关注商家
        /// </summary>
        public void FollowMerchant(Guid merchantId)
        {
            if (FollowedMerchants.Any(f => f.MerchantId == merchantId))
                throw new InvalidOperationException("已经关注过该商家");

            var follow = new UserFollow(Id, merchantId);
            FollowedMerchants.Add(follow);
            UpdateTimestamp();
        }

        /// <summary>
        /// 取消关注商家
        /// </summary>
        public void UnfollowMerchant(Guid merchantId)
        {
            var follow = FollowedMerchants.FirstOrDefault(f => f.MerchantId == merchantId);
            if (follow != null)
            {
                FollowedMerchants.Remove(follow);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 检查是否已关注商家
        /// </summary>
        public bool HasFollowedMerchant(Guid merchantId)
            => FollowedMerchants.Any(f => f.MerchantId == merchantId);

        // ============ 历史相关方法 ============

        /// <summary>
        /// 记录轴承浏览
        /// </summary>
        public void RecordBearingView(Guid bearingId)
        {
            var existing = BearingHistory.FirstOrDefault(h => h.BearingId == bearingId);
            if (existing != null)
            {
                existing.UpdateViewTime();
            }
            else
            {
                BearingHistory.Add(new UserBearingHistory(Id, bearingId));
            }

            // 限制历史记录数量（最多保留50条）
            if (BearingHistory.Count > 50)
            {
                var oldest = BearingHistory.OrderBy(h => h.ViewedAt).First();
                BearingHistory.Remove(oldest);
            }

            UpdateTimestamp();
        }

        /// <summary>
        /// 记录商家浏览
        /// </summary>
        public void RecordMerchantView(Guid merchantId)
        {
            var existing = MerchantHistory.FirstOrDefault(h => h.MerchantId == merchantId);
            if (existing != null)
            {
                existing.UpdateViewTime();
            }
            else
            {
                MerchantHistory.Add(new UserMerchantHistory(Id, merchantId));
            }

            // 限制历史记录数量
            if (MerchantHistory.Count > 50)
            {
                var oldest = MerchantHistory.OrderBy(h => h.ViewedAt).First();
                MerchantHistory.Remove(oldest);
            }

            UpdateTimestamp();
        }

        /// <summary>
        /// 清空浏览历史
        /// </summary>
        public void ClearHistory()
        {
            BearingHistory.Clear();
            MerchantHistory.Clear();
            UpdateTimestamp();
        }

        // ============ 辅助属性 ============

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

        /// <summary>
        /// 更新用户类型
        /// </summary>
        public void UpdateUserType(UserType newType)
        {
            if (UserType == newType) return;

            // 可以添加一些业务规则
            if (UserType == UserType.Admin && newType != UserType.Admin)
            {
                // 检查是否是最后一个管理员等
            }

            UserType = newType;
            UpdateTimestamp();
        }
    }
}

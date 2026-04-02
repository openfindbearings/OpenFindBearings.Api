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
        /// 认证系统用户ID (来自 OpenIddict)
        /// </summary>
        public string AuthUserId { get; private set; } = string.Empty;

        /// <summary>
        /// 用户昵称（展示用）
        /// </summary>
        public string? Nickname { get; private set; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string? Avatar { get; private set; }

        /// <summary>
        /// 用户类型：Admin/MerchantStaff/Individual/Guest
        /// </summary>
        public UserType UserType { get; private set; }

        /// <summary>
        /// 地址（业务数据）
        /// </summary>
        public string? Address { get; private set; }

        /// <summary>
        /// 游客会话ID（仅游客用户）
        /// </summary>
        public string? GuestSessionId { get; private set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        /// <summary>
        /// 是否活跃（软删除）
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// 是否已合并（游客数据已迁移）
        /// </summary>
        public bool IsMerged { get; private set; }

        /// <summary>
        /// 合并到的正式账户ID
        /// </summary>
        public Guid? MergedToUserId { get; private set; }

        /// <summary>
        /// 所属商家ID（仅商家员工）
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

        /// <summary>
        /// 收藏的轴承
        /// </summary>
        public List<UserBearingFavorite> FavoriteBearings { get; private set; } = [];

        /// <summary>
        /// 关注的商家
        /// </summary>
        public List<UserMerchantFollow> FollowedMerchants { get; private set; } = [];

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

        public int FavoriteCount => FavoriteBearings.Count;
        public int FollowCount => FollowedMerchants.Count;

        // ============ 构造函数 ============

        private User() { }

        /// <summary>
        /// 创建正式用户
        /// </summary>
        public User(string authUserId, UserType userType, string? nickname = null)
        {
            if (string.IsNullOrWhiteSpace(authUserId))
                throw new ArgumentException("认证用户ID不能为空", nameof(authUserId));

            AuthUserId = authUserId;
            UserType = userType;
            Nickname = nickname;
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

        // ============ 业务方法 ============

        /// <summary>
        /// 更新用户资料（只更新业务字段）
        /// </summary>
        public void UpdateProfile(string? nickname, string? avatar, string? address)
        {
            Nickname = nickname;
            Avatar = avatar;
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
        /// 启用用户
        /// </summary>
        public void Enable()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public void Disable()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        /// <summary>
        /// 标记为已合并（游客数据已迁移到正式账户）
        /// </summary>
        public void MarkAsMerged(Guid mergedToUserId)
        {
            IsMerged = true;
            MergedToUserId = mergedToUserId;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新用户类型
        /// </summary>
        public void UpdateUserType(UserType newType)
        {
            if (UserType == newType) return;
            UserType = newType;
            UpdateTimestamp();
        }

        // ============ 收藏相关方法 ============

        public void FavoriteBearing(Guid bearingId)
        {
            if (FavoriteBearings.Any(f => f.BearingId == bearingId))
                throw new InvalidOperationException("已经收藏过该轴承");

            FavoriteBearings.Add(new UserBearingFavorite(Id, bearingId));
            UpdateTimestamp();
        }

        public void UnfavoriteBearing(Guid bearingId)
        {
            var favorite = FavoriteBearings.FirstOrDefault(f => f.BearingId == bearingId);
            if (favorite != null)
            {
                FavoriteBearings.Remove(favorite);
                UpdateTimestamp();
            }
        }

        public bool HasFavoritedBearing(Guid bearingId)
            => FavoriteBearings.Any(f => f.BearingId == bearingId);

        // ============ 关注相关方法 ============

        public void FollowMerchant(Guid merchantId)
        {
            if (FollowedMerchants.Any(f => f.MerchantId == merchantId))
                throw new InvalidOperationException("已经关注过该商家");

            FollowedMerchants.Add(new UserMerchantFollow(Id, merchantId));
            UpdateTimestamp();
        }

        public void UnfollowMerchant(Guid merchantId)
        {
            var follow = FollowedMerchants.FirstOrDefault(f => f.MerchantId == merchantId);
            if (follow != null)
            {
                FollowedMerchants.Remove(follow);
                UpdateTimestamp();
            }
        }

        public bool HasFollowedMerchant(Guid merchantId)
            => FollowedMerchants.Any(f => f.MerchantId == merchantId);

        // ============ 历史相关方法 ============

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

            if (BearingHistory.Count > 50)
            {
                var oldest = BearingHistory.OrderBy(h => h.ViewedAt).First();
                BearingHistory.Remove(oldest);
            }
            UpdateTimestamp();
        }

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

            if (MerchantHistory.Count > 50)
            {
                var oldest = MerchantHistory.OrderBy(h => h.ViewedAt).First();
                MerchantHistory.Remove(oldest);
            }
            UpdateTimestamp();
        }

        public void ClearHistory()
        {
            BearingHistory.Clear();
            MerchantHistory.Clear();
            UpdateTimestamp();
        }

        // ============ 辅助属性 ============

        public bool IsMerchantStaff => UserType == UserType.MerchantStaff && MerchantId.HasValue;
        public bool IsAdmin => UserType == UserType.Admin;
        public bool IsGuest => UserType == UserType.Guest;
    }
}

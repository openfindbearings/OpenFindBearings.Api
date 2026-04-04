using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Domain.Aggregates
{
    /// <summary>
    /// 用户-聚合根
    /// 业务系统中的用户，与认证系统的用户通过 AuthUserId 关联
    /// 认证服务负责：手机号、微信OpenId、密码等认证信息
    /// 业务服务负责：用户类型、等级、画像、行为统计等业务数据
    /// </summary>
    public class User : BaseEntity
    {
        // ============ 关联字段 ============

        /// <summary>
        /// 认证系统用户ID (来自 OpenIddict)
        /// 与认证服务关联的唯一标识
        /// </summary>
        public string AuthUserId { get; private set; } = string.Empty;

        // ============ 基础信息 ============

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

        // ============ 会员信息 ============

        /// <summary>
        /// 用户等级（免费/付费/企业）
        /// </summary>
        public UserLevel Level { get; private set; } = UserLevel.Free;

        /// <summary>
        /// 付费到期时间
        /// </summary>
        public DateTime? SubscriptionExpiry { get; private set; }

        // ============ 注册信息 ============

        /// <summary>
        /// 注册来源（WeChat/Mobile/Web/Admin）
        /// </summary>
        public RegistrationSource RegistrationSource { get; private set; }

        /// <summary>
        /// 注册IP
        /// </summary>
        public string? RegisterIp { get; private set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegisteredAt { get; private set; }

        // ============ 用户画像 ============

        /// <summary>
        /// 用户职业（采购员/销售员/工程师/其他）
        /// </summary>
        public UserOccupation? Occupation { get; private set; }

        /// <summary>
        /// 公司名称（可选）
        /// </summary>
        public string? CompanyName { get; private set; }

        /// <summary>
        /// 行业类型
        /// </summary>
        public string? Industry { get; private set; }

        // ============ 行为统计 ============

        /// <summary>
        /// 总搜索次数
        /// </summary>
        public int SearchCount { get; private set; }

        /// <summary>
        /// 总查询次数
        /// </summary>
        public int QueryCount { get; private set; }

        /// <summary>
        /// 首次搜索时间
        /// </summary>
        public DateTime? FirstSearchAt { get; private set; }

        /// <summary>
        /// 最后搜索时间
        /// </summary>
        public DateTime? LastSearchAt { get; private set; }

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime? LastActiveAt { get; private set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        // ============ 状态字段 ============

        /// <summary>
        /// 是否启用（软删除）
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// 是否已合并（游客数据已迁移到正式账户）
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

        // ============ 导航属性（管理的子实体） ============

        /// <summary>
        /// 用户-角色关联导航属性
        /// </summary>
        private readonly List<UserRole> _userRoles = [];
        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        /// <summary>
        /// 收藏的轴承
        /// </summary>
        private readonly List<UserBearingFavorite> _favoriteBearings = [];
        public IReadOnlyCollection<UserBearingFavorite> FavoriteBearings => _favoriteBearings.AsReadOnly();

        /// <summary>
        /// 关注的商家
        /// </summary>
        private readonly List<UserMerchantFollow> _followedMerchants = [];
        public IReadOnlyCollection<UserMerchantFollow> FollowedMerchants => _followedMerchants.AsReadOnly();

        /// <summary>
        /// 轴承浏览历史
        /// </summary>
        private readonly List<UserBearingHistory> _bearingHistory = [];
        public IReadOnlyCollection<UserBearingHistory> BearingHistory => _bearingHistory.AsReadOnly();

        /// <summary>
        /// 商家浏览历史
        /// </summary>
        private readonly List<UserMerchantHistory> _merchantHistory = [];
        public IReadOnlyCollection<UserMerchantHistory> MerchantHistory => _merchantHistory.AsReadOnly();

        /// <summary>
        /// 提交的纠错
        /// </summary>
        private readonly List<CorrectionRequest> _submittedCorrections = [];
        public IReadOnlyCollection<CorrectionRequest> SubmittedCorrections => _submittedCorrections.AsReadOnly();

        // ============ 统计属性 ============

        public int FavoriteCount => _favoriteBearings.Count;
        public int FollowCount => _followedMerchants.Count;

        public bool IsMerchantStaff => UserType == UserType.MerchantStaff && MerchantId.HasValue;
        public bool IsAdmin => UserType == UserType.Admin;
        public bool IsGuest => UserType == UserType.Guest;

        // ============ 构造函数 ============

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private User() { }

        /// <summary>
        /// 创建正式用户（认证服务已创建认证用户后调用）
        /// </summary>
        /// <param name="authUserId">认证系统用户ID</param>
        /// <param name="userType">用户类型</param>
        /// <param name="registrationSource">注册来源</param>
        /// <param name="registerIp">注册IP</param>
        /// <param name="nickname">昵称</param>
        public User(
            string authUserId,
            UserType userType,
            RegistrationSource registrationSource,
            string? registerIp = null,
            string? nickname = null)
        {
            if (string.IsNullOrWhiteSpace(authUserId))
                throw new ArgumentException("认证用户ID不能为空", nameof(authUserId));

            AuthUserId = authUserId;
            UserType = userType;
            RegistrationSource = registrationSource;
            RegisterIp = registerIp;
            Nickname = nickname;
            RegisteredAt = DateTime.UtcNow;
            LastActiveAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 创建游客用户
        /// </summary>
        /// <param name="guestSessionId">游客会话ID</param>
        /// <param name="registerIp">注册IP</param>
        public User(string guestSessionId, string? registerIp = null)
        {
            if (string.IsNullOrWhiteSpace(guestSessionId))
                throw new ArgumentException("游客会话ID不能为空", nameof(guestSessionId));

            GuestSessionId = guestSessionId;
            UserType = UserType.Guest;
            RegistrationSource = RegistrationSource.Guest;
            RegisterIp = registerIp;
            RegisteredAt = DateTime.UtcNow;
            LastActiveAt = DateTime.UtcNow;
        }

        // ============ 业务方法 ============

        /// <summary>
        /// 记录登录
        /// 由认证服务调用，每次用户登录时触发
        /// </summary>
        public void RecordLogin(string? loginIp = null, string? userAgent = null, string? loginMethod = null)
        {
            LastLoginAt = DateTime.UtcNow;
            LastActiveAt = DateTime.UtcNow;
            UpdateTimestamp();

            AddDomainEvent(new UserLoggedInEvent(Id, AuthUserId, loginMethod ?? "Unknown", loginIp, userAgent));
        }

        /// <summary>
        /// 记录注册（首次登录时）
        /// </summary>
        public static User CreateFromAuth(string authUserId, RegistrationSource source,
            string? registerIp = null, string? nickname = null)
        {
            var user = new User(authUserId, UserType.Individual, source, registerIp, nickname);
            user.AddDomainEvent(new UserRegisteredEvent(user.Id, authUserId, source, registerIp));
            return user;
        }

        /// <summary>
        /// 记录搜索行为
        /// </summary>
        public void RecordSearch()
        {
            SearchCount++;
            LastSearchAt = DateTime.UtcNow;
            if (!FirstSearchAt.HasValue) FirstSearchAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 记录查询行为
        /// </summary>
        public void RecordQuery()
        {
            QueryCount++;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新最后活跃时间
        /// </summary>
        public void UpdateLastActive()
        {
            LastActiveAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新用户资料
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
        /// 更新用户画像（职业、公司、行业）
        /// </summary>
        public void UpdateUserProfile(UserOccupation occupation, string? companyName = null, string? industry = null)
        {
            Occupation = occupation;
            CompanyName = companyName;
            Industry = industry;
            UpdateTimestamp();
        }

        /// <summary>
        /// 升级为付费用户
        /// </summary>
        public void UpgradeToPremium(DateTime expiry)
        {
            Level = UserLevel.Premium;
            SubscriptionExpiry = expiry;
            UpdateTimestamp();
        }

        /// <summary>
        /// 升级为企业用户
        /// </summary>
        public void UpgradeToEnterprise()
        {
            Level = UserLevel.Enterprise;
            UpdateTimestamp();
        }

        /// <summary>
        /// 降级为免费用户
        /// </summary>
        public void DowngradeToFree()
        {
            Level = UserLevel.Free;
            SubscriptionExpiry = null;
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

        // ============ 角色管理 ============

        /// <summary>
        /// 添加角色
        /// </summary>
        internal void AddRole(Guid roleId)
        {
            if (!_userRoles.Any(r => r.RoleId == roleId))
            {
                _userRoles.Add(new UserRole(Id, roleId));
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除角色
        /// </summary>
        internal void RemoveRole(Guid roleId)
        {
            var role = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
            if (role != null)
            {
                _userRoles.Remove(role);
                UpdateTimestamp();
            }
        }

        // ============ 收藏相关方法 ============

        /// <summary>
        /// 收藏轴承
        /// </summary>
        public void FavoriteBearing(Guid bearingId)
        {
            if (_favoriteBearings.Any(f => f.BearingId == bearingId))
                throw new InvalidOperationException("已经收藏过该轴承");

            _favoriteBearings.Add(new UserBearingFavorite(Id, bearingId));
            UpdateTimestamp();
        }

        /// <summary>
        /// 取消收藏轴承
        /// </summary>
        public void UnfavoriteBearing(Guid bearingId)
        {
            var favorite = _favoriteBearings.FirstOrDefault(f => f.BearingId == bearingId);
            if (favorite != null)
            {
                _favoriteBearings.Remove(favorite);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 是否已收藏轴承
        /// </summary>
        public bool HasFavoritedBearing(Guid bearingId)
            => _favoriteBearings.Any(f => f.BearingId == bearingId);

        // ============ 关注相关方法 ============

        /// <summary>
        /// 关注商家
        /// </summary>
        public void FollowMerchant(Guid merchantId)
        {
            if (_followedMerchants.Any(f => f.MerchantId == merchantId))
                throw new InvalidOperationException("已经关注过该商家");

            _followedMerchants.Add(new UserMerchantFollow(Id, merchantId));
            UpdateTimestamp();
        }

        /// <summary>
        /// 取消关注商家
        /// </summary>
        public void UnfollowMerchant(Guid merchantId)
        {
            var follow = _followedMerchants.FirstOrDefault(f => f.MerchantId == merchantId);
            if (follow != null)
            {
                _followedMerchants.Remove(follow);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 是否已关注商家
        /// </summary>
        public bool HasFollowedMerchant(Guid merchantId)
            => _followedMerchants.Any(f => f.MerchantId == merchantId);

        // ============ 历史相关方法 ============

        /// <summary>
        /// 记录轴承浏览
        /// </summary>
        public void RecordBearingView(Guid bearingId)
        {
            var existing = _bearingHistory.FirstOrDefault(h => h.BearingId == bearingId);
            if (existing != null)
            {
                existing.UpdateViewTime();
            }
            else
            {
                _bearingHistory.Add(new UserBearingHistory(Id, bearingId));
            }

            // 保留最近50条记录
            if (_bearingHistory.Count > 50)
            {
                var oldest = _bearingHistory.OrderBy(h => h.ViewedAt).First();
                _bearingHistory.Remove(oldest);
            }
            UpdateTimestamp();
        }

        /// <summary>
        /// 记录商家浏览
        /// </summary>
        public void RecordMerchantView(Guid merchantId)
        {
            var existing = _merchantHistory.FirstOrDefault(h => h.MerchantId == merchantId);
            if (existing != null)
            {
                existing.UpdateViewTime();
            }
            else
            {
                _merchantHistory.Add(new UserMerchantHistory(Id, merchantId));
            }

            // 保留最近50条记录
            if (_merchantHistory.Count > 50)
            {
                var oldest = _merchantHistory.OrderBy(h => h.ViewedAt).First();
                _merchantHistory.Remove(oldest);
            }
            UpdateTimestamp();
        }

        /// <summary>
        /// 清空浏览历史
        /// </summary>
        public void ClearHistory()
        {
            _bearingHistory.Clear();
            _merchantHistory.Clear();
            UpdateTimestamp();
        }
    }
}

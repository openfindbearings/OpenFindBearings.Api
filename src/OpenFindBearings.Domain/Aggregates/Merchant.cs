using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Aggregates
{
    /// <summary>
    /// 商家-聚合根
    /// </summary>
    public class Merchant : BaseEntity
    {
        // ============ 基本属性 ============

        /// <summary>
        /// 商家名称（展示用）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 公司全称（营业执照上的正式名称）
        /// </summary>
        public string? CompanyName { get; private set; }

        /// <summary>
        /// 统一社会信用代码
        /// </summary>
        public string? UnifiedSocialCreditCode { get; private set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public MerchantType Type { get; private set; }

        /// <summary>
        /// 联系方式（值对象）
        /// </summary>
        public ContactInfo Contact { get; private set; }

        /// <summary>
        /// 商家简介
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 经营范围
        /// </summary>
        public string? BusinessScope { get; private set; }

        /// <summary>
        /// 商家Logo URL
        /// </summary>
        public string? LogoUrl { get; private set; }

        /// <summary>
        /// 商家官网
        /// </summary>
        public string? Website { get; private set; }

        // ============ 认证与等级 ============

        /// <summary>
        /// 是否已通过平台认证
        /// </summary>
        public bool IsVerified { get; private set; }

        /// <summary>
        /// 认证通过时间
        /// </summary>
        public DateTime? VerifiedAt { get; private set; }

        /// <summary>
        /// 商家等级
        /// </summary>
        public MerchantGrade Grade { get; private set; }

        /// <summary>
        /// 商家状态
        /// </summary>
        public MerchantStatus Status { get; private set; }

        /// <summary>
        /// 禁用原因（当 Status = Suspended 时）
        /// </summary>
        public string? SuspensionReason { get; private set; }

        // ============ 统计字段 ============

        /// <summary>
        /// 产品总数
        /// </summary>
        public int ProductCount { get; private set; }

        /// <summary>
        /// 关注者数量
        /// </summary>
        public int FollowerCount { get; private set; }

        /// <summary>
        /// 总浏览次数
        /// </summary>
        public int ViewCount { get; private set; }

        // ============ 数据追溯字段 ============

        /// <summary>
        /// 数据来源信息
        /// </summary>
        public DataSource? DataSource { get; private set; }

        /// <summary>
        /// 最后校验时间
        /// </summary>
        public DateTime? LastVerifiedAt { get; private set; }

        /// <summary>
        /// 校验人/系统
        /// </summary>
        public string? VerifiedBy { get; private set; }

        /// <summary>
        /// 是否为已验证数据
        /// </summary>
        public bool IsDataVerified { get; private set; }

        /// <summary>
        /// 数据备注
        /// </summary>
        public string? DataRemark { get; private set; }

        // ============ 导航属性 ============

        /// <summary>
        /// 员工列表
        /// </summary>
        private readonly List<User> _staff = [];
        public IReadOnlyCollection<User> Staff => _staff.AsReadOnly();

        /// <summary>
        /// 产品目录
        /// </summary>
        private readonly List<MerchantBearing> _merchantBearings = [];
        public IReadOnlyCollection<MerchantBearing> MerchantBearings => _merchantBearings.AsReadOnly();

        /// <summary>
        /// 关注此商家的用户
        /// </summary>
        private readonly List<UserMerchantFollow> _followedByUsers = [];
        public IReadOnlyCollection<UserMerchantFollow> FollowedByUsers => _followedByUsers.AsReadOnly();

        /// <summary>
        /// 营业执照审核记录
        /// </summary>
        private readonly List<LicenseVerification> _licenseVerifications = [];
        public IReadOnlyCollection<LicenseVerification> LicenseVerifications => _licenseVerifications.AsReadOnly();

        // ============ 构造函数 ============

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private Merchant() 
        {
            Name = string.Empty;
            Contact = null!;
        }

        /// <summary>
        /// 创建新商家
        /// </summary>
        public Merchant(
            string name,
            MerchantType type,
            ContactInfo contact)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("商家名称不能为空", nameof(name));

            Name = name;
            Type = type;
            Contact = contact ?? throw new ArgumentNullException(nameof(contact));
            Grade = MerchantGrade.Standard;
            Status = MerchantStatus.Pending;  // 新商家默认待审核
        }

        /// <summary>
        /// 创建商家（用于爬虫/导入）
        /// </summary>
        public static Merchant CreateFromSource(
            string name,
            MerchantType type,
            ContactInfo contact,
            DataSource dataSource)
        {
            var merchant = new Merchant(name, type, contact);
            merchant.SetDataSource(dataSource);
            return merchant;
        }

        // ============ 基本信息更新 ============

        /// <summary>
        /// 更新基本信息
        /// </summary>
        public void UpdateBasicInfo(
            string? companyName,
            string? unifiedSocialCreditCode,
            string? description,
            string? businessScope,
            string? logoUrl,
            string? website)
        {
            CompanyName = companyName;
            UnifiedSocialCreditCode = unifiedSocialCreditCode;
            Description = description;
            BusinessScope = businessScope;
            LogoUrl = logoUrl;
            Website = website;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新商家名称
        /// </summary>
        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("商家名称不能为空", nameof(newName));

            Name = newName;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新联系方式
        /// </summary>
        public void UpdateContact(ContactInfo newContact)
        {
            Contact = newContact ?? throw new ArgumentNullException(nameof(newContact));
            UpdateTimestamp();
        }

        // ============ 状态管理 ============

        /// <summary>
        /// 审核通过（管理员调用）
        /// </summary>
        public void Approve()
        {
            if (Status != MerchantStatus.Pending)
                throw new InvalidOperationException($"当前状态为 {Status}，无法审核");

            Status = MerchantStatus.Active;
            UpdateTimestamp();

            AddDomainEvent(new MerchantVerifiedEvent(Id, Name));
        }

        /// <summary>
        /// 审核拒绝（管理员调用）
        /// </summary>
        public void Reject(string reason)
        {
            if (Status != MerchantStatus.Pending)
                throw new InvalidOperationException($"当前状态为 {Status}，无法审核");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("拒绝原因不能为空", nameof(reason));

            Status = MerchantStatus.Suspended;
            SuspensionReason = reason;
            Deactivate();  // 基类方法
            UpdateTimestamp();
        }

        /// <summary>
        /// 认证商家
        /// </summary>
        public void Verify()
        {
            if (IsVerified)
                throw new InvalidOperationException("商家已经认证");

            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;

            // 认证后升级等级
            if (Grade < MerchantGrade.Verified)
                Grade = MerchantGrade.Verified;

            UpdateTimestamp();

            AddDomainEvent(new MerchantVerifiedEvent(Id, Name));
        }

        /// <summary>
        /// 取消认证
        /// </summary>
        public void Unverify()
        {
            if (!IsVerified)
                throw new InvalidOperationException("商家未认证");

            IsVerified = false;
            VerifiedAt = null;
            UpdateTimestamp();

            AddDomainEvent(new MerchantUnverifiedEvent(Id, Name));
        }

        /// <summary>
        /// 更新商家等级
        /// </summary>
        public void UpdateGrade(MerchantGrade newGrade)
        {
            if (newGrade == MerchantGrade.Unknown)
                throw new ArgumentException("等级不能为 Unknown", nameof(newGrade));

            var oldGrade = Grade;
            Grade = newGrade;
            UpdateTimestamp();

            AddDomainEvent(new MerchantGradeChangedEvent(Id, oldGrade, newGrade));
        }

        /// <summary>
        /// 禁用商家（管理员强制禁用）
        /// </summary>
        public void Suspend(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("禁用原因不能为空", nameof(reason));

            if (Status == MerchantStatus.Suspended)
                return;

            Status = MerchantStatus.Suspended;
            SuspensionReason = reason;
            Deactivate();  // 基类方法
            UpdateTimestamp();
        }

        /// <summary>
        /// 启用商家
        /// </summary>
        public void ActivateMerchant()
        {
            if (Status == MerchantStatus.Active)
                return;

            Status = MerchantStatus.Active;
            SuspensionReason = null;
            Activate();  // 基类方法
            UpdateTimestamp();
        }

        // ============ 员工管理 ============

        /// <summary>
        /// 添加员工
        /// </summary>
        internal void AddStaff(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!_staff.Contains(user))
            {
                _staff.Add(user);
                user.AssignToMerchant(Id);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除员工
        /// </summary>
        internal void RemoveStaff(User user)
        {
            if (user != null && _staff.Contains(user))
            {
                _staff.Remove(user);
                user.RemoveFromMerchant();
                UpdateTimestamp();
            }
        }

        // ============ 产品管理 ============

        /// <summary>
        /// 添加产品关联
        /// </summary>
        internal void AddMerchantBearing(MerchantBearing merchantBearing)
        {
            if (merchantBearing == null)
                throw new ArgumentNullException(nameof(merchantBearing));

            if (!_merchantBearings.Any(mb => mb.BearingId == merchantBearing.BearingId))
            {
                _merchantBearings.Add(merchantBearing);
                UpdateProductCount();
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除产品关联
        /// </summary>
        internal void RemoveMerchantBearing(Guid bearingId)
        {
            var mb = _merchantBearings.FirstOrDefault(x => x.BearingId == bearingId);
            if (mb != null)
            {
                _merchantBearings.Remove(mb);
                UpdateProductCount();
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 更新产品数量统计
        /// </summary>
        private void UpdateProductCount()
        {
            ProductCount = _merchantBearings.Count(mb => mb.IsOnSale);
        }

        // ============ 关注管理 ============

        /// <summary>
        /// 增加关注者
        /// </summary>
        internal void AddFollower(UserMerchantFollow follow)
        {
            if (!_followedByUsers.Any(f => f.UserId == follow.UserId))
            {
                _followedByUsers.Add(follow);
                FollowerCount = _followedByUsers.Count;
                UpdateTimestamp();

                AddDomainEvent(new MerchantFollowedEvent(follow.UserId, Id));
            }
        }

        /// <summary>
        /// 移除关注者
        /// </summary>
        internal void RemoveFollower(Guid userId)
        {
            var follow = _followedByUsers.FirstOrDefault(f => f.UserId == userId);
            if (follow != null)
            {
                _followedByUsers.Remove(follow);
                FollowerCount = _followedByUsers.Count;
                UpdateTimestamp();

                AddDomainEvent(new MerchantUnfollowedEvent(userId, Id));
            }
        }

        // ============ 统计更新 ============

        /// <summary>
        /// 增加浏览次数
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
            UpdateTimestamp();
        }

        // ============ 数据来源管理 ============

        /// <summary>
        /// 设置数据来源
        /// </summary>
        public void SetDataSource(DataSource dataSource)
        {
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            UpdateTimestamp();
        }

        /// <summary>
        /// 标记数据为已验证
        /// </summary>
        public void MarkDataAsVerified(string? verifiedBy = null)
        {
            IsDataVerified = true;
            LastVerifiedAt = DateTime.UtcNow;
            VerifiedBy = verifiedBy;
            UpdateTimestamp();
        }

        /// <summary>
        /// 添加数据备注
        /// </summary>
        public void AddDataRemark(string remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
                throw new ArgumentException("备注不能为空", nameof(remark));

            DataRemark = string.IsNullOrEmpty(DataRemark)
                ? remark
                : $"{DataRemark}; {remark}";
            UpdateTimestamp();
        }

        // ============ 营业执照审核 ============

        /// <summary>
        /// 添加营业执照审核记录
        /// </summary>
        internal void AddLicenseVerification(LicenseVerification verification)
        {
            _licenseVerifications.Add(verification);
            UpdateTimestamp();
        }

        // ============ 查询方法 ============

        /// <summary>
        /// 获取商家摘要
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string> { Name };
            if (IsVerified) parts.Add("[已认证]");
            parts.Add($"{GetGradeDisplayName()}商家");
            parts.Add($"{ProductCount}个产品");
            return string.Join(" | ", parts);
        }

        /// <summary>
        /// 判断是否有效商家
        /// </summary>
        public bool IsValid => Status == MerchantStatus.Active && IsActive;

        /// <summary>
        /// 获取商家类型显示名称
        /// </summary>
        public string GetMerchantTypeDisplayName() => Type switch
        {
            MerchantType.Manufacturer => "生产厂家",
            MerchantType.AuthorizedDealer => "授权经销商",
            MerchantType.Distributor => "分销商",
            MerchantType.Trader => "贸易商",
            _ => "其他"
        };

        /// <summary>
        /// 获取商家等级显示名称
        /// </summary>
        public string GetGradeDisplayName() => Grade switch
        {
            MerchantGrade.Standard => "标准商家",
            MerchantGrade.Premium => "优质商家",
            MerchantGrade.Verified => "认证商家",
            _ => "未评级"
        };
    }
}

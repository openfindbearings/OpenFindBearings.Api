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
        /// 英文名称（用于国际化展示）
        /// </summary>
        public string? EnglishName { get; private set; }

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

        /// <summary>
        /// 入驻渠道标记（Self/Claim/Nomination），用于申请人撤回时决定"删商户"还是"退回爬虫"。
        /// 改动说明：新增，默认 None 兼容爬虫/种子/历史数据。
        /// </summary>
        public ApplicationMode ApplicationMode { get; private set; }

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

        // 改动说明：移除 Staff 导航（原 List<User>，依赖已废弃的 User.MerchantId 单值反向关系）；
        //   商户成员改由独立的 MerchantMember 表建模

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
        /// 设置英文名称
        /// </summary>
        public void SetEnglishName(string? englishName)
        {
            EnglishName = englishName;
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

            // 改动说明：补发"入驻审核通过"事件（站内信订阅者据此通知管理员）；
            //   原 MerchantVerifiedEvent 语义是资质认证，与入驻生效不同轴，保留以兼容既有订阅
            AddDomainEvent(new MerchantApprovedEvent(Id, Name));
            AddDomainEvent(new MerchantVerifiedEvent(Id, Name));
        }

        /// <summary>
        /// 标记为提名草稿（提名他人为管理员时使用，Draft 状态不可被C端检索、不被Sync合并）
        /// </summary>
        public void MarkAsDraft()
        {
            Status = MerchantStatus.Draft;
            UpdateTimestamp();
        }

        /// <summary>
        /// 提交审核（Draft -> Pending，提名被接受后由被提名人补全资料并提交）
        /// </summary>
        public void SubmitForApproval()
        {
            if (Status != MerchantStatus.Draft)
                throw new InvalidOperationException($"当前状态为 {Status}，无法提交审核");

            Status = MerchantStatus.Pending;
            UpdateTimestamp();
        }

        /// <summary>
        /// 重新开放认领（修复 B7：曾被拒绝的爬虫商家再次被认领时，
        /// 清除拒绝标记恢复为待审核，重新走审核流程）
        /// </summary>
        public void ReopenForClaim()
        {
            if (Status != MerchantStatus.Suspended)
                return;

            Status = MerchantStatus.Pending;
            SuspensionReason = null;
            Activate();  // 基类方法，恢复为有效（拒绝时曾被 Deactivate）
            UpdateTimestamp();
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

            // 改动说明：补发"入驻审核拒绝"事件，携带原因供站内信正文展示
            AddDomainEvent(new MerchantRejectedEvent(Id, Name, reason));
        }

        /// <summary>
        /// 认证商家
        /// </summary>
        public void Verify(string? verifiedBy = null)
        {
            if (IsVerified)
                throw new InvalidOperationException("商家已经认证");

            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            VerifiedBy = verifiedBy;

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

        /// <summary>
        /// 标记入驻渠道（Self/Claim/Nomination），供申请人撤回时判定清理策略
        /// </summary>
        public void MarkApplicationMode(ApplicationMode mode)
        {
            ApplicationMode = mode;
            UpdateTimestamp();
        }

        /// <summary>
        /// 认领撤回：把商户退回"爬虫自有"状态——来源退回 Crawler（重新可被 Sync 覆盖、重新进入认领池），
        /// 渠道标记归 None。改动说明：认领时曾置 Manual 以护住认领人编辑，撤回后应还原为可被爬虫刷写。
        /// </summary>
        /// <param name="crawlerSiteName">回退后归属的爬虫站点名（占位标识即可）</param>
        public void RevertClaimedToCrawler(string crawlerSiteName)
        {
            SetDataSource(DataSource.FromCrawler(crawlerSiteName));
            ApplicationMode = ApplicationMode.None;
            UpdateTimestamp();
        }

        // 改动说明：移除原"员工管理"节（AddStaff/RemoveStaff + _staff 集合）——
        //   它们依赖已废弃的 User.MerchantId 单值关系；商户成员改由 MerchantMember 成员表承载（见成员仓储与端点）

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
        public void AddLicenseVerification(LicenseVerification verification)
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

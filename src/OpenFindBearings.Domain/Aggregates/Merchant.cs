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
        /// 商户是否已主动申请认证（v2.9.0：申请-审核闭环信号，Admin 认证/驳回后清除；
        /// 认证资格校验以材料矩阵为准，本标记仅作审核队列优先级提示）
        /// </summary>
        public bool VerifyRequested { get; private set; }

        /// <summary>
        /// 认证通过时间
        /// </summary>
        public DateTime? VerifiedAt { get; private set; }

        /// <summary>
        /// 入驻生效时间（审核通过时写入，UTC）。
        /// 改动说明（v1.50.0 入驻统计）：用于区分"互联网同步新增"（CreatedAt）与"入驻新增"（含认领既有商家）；
        /// 为 null 表示尚未入驻生效（互联网/种子/历史未认领数据）。
        /// </summary>
        public DateTime? OnboardedAt { get; private set; }

        /// <summary>
        /// 商家等级
        /// </summary>
        public MerchantGrade Grade { get; private set; }

        /// <summary>
        /// 保级缓冲截止时刻（v2.12.0 等级玩法）：重算发现应降档时先挂此钟（now+宽限天数），
        /// 期内维持原等级与成员 buff；到期后仍不达标才实际降档。升档或无需降级时清空
        /// </summary>
        public DateTime? GradeGraceUntil { get; private set; }

        /// <summary>
        /// 商家状态
        /// </summary>
        public MerchantStatus Status { get; private set; }

        /// <summary>
        /// 禁用原因（当 Status = Suspended 时）
        /// </summary>
        public string? SuspensionReason { get; private set; }

        /// <summary>
        /// 入驻渠道标记（Self/Claim/Nomination），用于申请人撤回时决定"删商户"还是"退回互联网"。
        /// 改动说明：新增，默认 None 兼容互联网/种子/历史数据。
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
        /// 证照材料审核记录（v2.7.0 由"营业执照审核记录"泛化：执照/授权书/厂房照多类型）
        /// 改动说明（v2.8.1）：字段名从 _documents 改为 _merchantDocuments——EF Core 按
        ///   "<下划线+属性名camel>"约定发现 backing field，原名不匹配导致 EF 导航修复
        ///   落到 getter 的 AsReadOnly() 包装上，随单材料 Add 时抛 Collection is read-only
        ///   （_merchantBearings/_followedByUsers 因命名合规从未出问题，即此佐证）
        /// </summary>
        private readonly List<MerchantDocument> _merchantDocuments = [];
        public IReadOnlyCollection<MerchantDocument> MerchantDocuments => _merchantDocuments.AsReadOnly();

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
            // 改动说明（v2.18.0 卡级重构）：初始档由"入驻商家"改为"注册商户"（未认证即无卡级）
            Grade = MerchantGrade.Registered;
            Status = MerchantStatus.Pending;  // 新商家默认待审核
        }

        /// <summary>
        /// 创建商家（用于互联网/导入）
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
            // 改动说明（v1.50.0 入驻时间）：入驻生效即记时（`??=` 幂等，重复审核不覆盖首次）；
            //   Self/Claim/Nomination 三条入驻路径都经本方法，故认领既有商家（CreatedAt 为老时间）也能被正确计时
            OnboardedAt ??= DateTime.UtcNow;
            UpdateTimestamp();

            // 改动说明：补发"入驻审核通过"事件（站内信订阅者据此通知管理员）；
            //   原 MerchantVerifiedEvent 语义是资质认证，与入驻生效不同轴，保留以兼容既有订阅
            AddDomainEvent(new MerchantApprovedEvent(Id, Name));
            AddDomainEvent(new MerchantVerifiedEvent(Id, Name));
        }

        /// <summary>
        /// 标记为提名草稿（提名他人为管理员时使用，Draft 状态不可被C端检索、不被互联网数据合并）
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
        /// 重新开放认领（修复 B7：曾被拒绝的互联网商家再次被认领时，
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
        /// 被拒后修改资料重新提交（v2.6.0 新增，self/claim 申请人本人通道）：
        /// 守卫 Suspended（仅被驳回的申请可重提），状态回 Pending、清驳回原因并恢复有效。
        /// 与 ReopenForClaim 的区别：本方法是"原申请人自己改完再交"，成员关系保留不动；
        /// ReopenForClaim 是"他人接盘重认领"，调用前需先解除旧成员。
        /// </summary>
        public void Resubmit()
        {
            if (Status != MerchantStatus.Suspended)
                throw new InvalidOperationException($"当前状态为 {Status}，无法重新提交");

            Status = MerchantStatus.Pending;
            SuspensionReason = null;
            Activate();  // 基类方法，恢复为有效（拒绝时曾被 Deactivate）
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新商家类型（v2.6.0 新增：重提编辑表单允许改类型；建店时 UpdateBasicInfo 不含 Type）
        /// </summary>
        public void UpdateType(MerchantType newType)
        {
            Type = newType;
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
        /// 商户主动申请认证（v2.9.0）：仅置意向标记，资格由调用方按材料矩阵校验；幂等可重复申请
        /// </summary>
        public void RequestVerify()
        {
            if (IsVerified)
                throw new InvalidOperationException("商家已经认证，无需申请");

            VerifyRequested = true;
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
            // 改动说明（v2.9.0）：认证完成即清除商户的待处理申请标记
            VerifyRequested = false;

            // 改动说明（v2.18.0 卡级重构）：认证通过即达"铜卡"（银/金卡由重算服务按在售+成功撮合上修）；
            // 认证是卡级门槛而非档位名——认证徽章独立展示（IsVerified）
            Grade = MerchantGrade.Bronze;
            // 改动说明（v2.12.0 等级玩法）：认证事件直接落级，历史保级钟作废
            GradeGraceUntil = null;

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
            // 改动说明（v2.18.0 卡级重构）：认证是卡级的硬前提，撤销认证卡级即回落"注册商户"
            // （回 Registered，后续供给事件会按规则重新上修）。撤销认证属平台执法动作，
            // 不走 v2.12.0 保级缓冲——直接落级并作废保级钟
            Grade = MerchantGrade.Registered;
            GradeGraceUntil = null;
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
            // 改动说明（v2.12.0 等级玩法）：等级一经落档，保级钟随之作废
            // （升档不需要缓冲；到期降档写回新等级后也应保持空钟等下一轮）
            GradeGraceUntil = null;
            UpdateTimestamp();

            AddDomainEvent(new MerchantGradeChangedEvent(Id, oldGrade, newGrade));
        }

        /// <summary>
        /// 挂起/清除保级缓冲（v2.12.0 等级玩法）：传时刻=重算发现不达标先挂钟保级，
        /// 商户端展示倒计时与回升指引；传 null=恢复达标或已落级后清钟
        /// </summary>
        /// <param name="graceUntilUtc">保级截止时刻（UTC；null=清除）</param>
        public void SetGradeGraceUntil(DateTime? graceUntilUtc)
        {
            GradeGraceUntil = graceUntilUtc;
            UpdateTimestamp();
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
        /// 认领撤回：把商户退回"互联网自有"状态——来源退回 Internet（重新可被 互联网数据覆盖、重新进入认领池），
        /// 渠道标记归 None。改动说明：认领时曾置 Manual 以护住认领人编辑，撤回后应还原为可被互联网刷写。
        /// </summary>
        /// <param name="internetSiteName">回退后归属的互联网站点名（占位标识即可）</param>
        public void RevertClaimedToInternet(string internetSiteName)
        {
            SetDataSource(DataSource.FromInternet(internetSiteName));
            ApplicationMode = ApplicationMode.None;
            UpdateTimestamp();
        }

        /// <summary>
        /// 解除归属回公海（v2.17.0 商户关店/Admin 强制解除）：Active→Pending + 认领相关轴全部重置，
        /// 实体存在性交还互联网管线裁判（互联网数据刷新 重置 staging 行后，下轮互联网经
        /// DataSource=Internet 覆盖通道刷新；源头页面消亡则自然不再更新）。
        /// 成员/商品/证照/邀请/纠错等关联行由 ApplicantApplicationCleanup.ResetOperationalDataForReleaseAsync 清理（聚合外职责）。
        /// 保留轴：CompanyName/UnifiedSocialCreditCode/Type/Description/Logo——入驻审批已核实或属公开信息，
        ///   互联网数据覆盖通道不会回填这些字段，清掉反而丢失经核实的主体信息且误导预填。
        /// 清空轴：Contact 全部（上一任认领人私人联系方式，管线刷新前不得在公海暴露——隐私止血）、
        ///   Website（认领人自填且 互联网数据不回填，错误值会误导买家）。
        /// </summary>
        /// <param name="internetSiteName">回退后归属的互联网站点占位标识</param>
        public void ReleaseToPool(string internetSiteName = "released")
        {
            // 守卫：仅生效商户可释放；Pending 走撤回、Suspended 走删申请，三态入口不混用
            if (Status != MerchantStatus.Active)
            {
                throw new InvalidOperationException($"仅生效商户可解除归属，当前状态：{Status}");
            }

            Status = MerchantStatus.Pending; // 公海待认领态：重新认领提交后必再走审核（与互联网新商户同构）
            IsActive = true;                 // 保持搜索可见（公海商户可被发现/认领）
            RevertClaimedToInternet(internetSiteName); // DataSource 回 Internet + ApplicationMode 归 None

            // 认证轴重置：认领期获得的认证状态/申请标记/等级不带入公海
            if (IsVerified)
            {
                Unverify(); // Unverify 未认证时抛异常，条件调用；内部清 VerifiedAt
            }
            else
            {
                VerifiedAt = null;
            }
            VerifyRequested = false;
            // 改动说明（v2.18.0 卡级重构）：枚举单调后"高于注册商户"即持卡级，回海域一律落回注册商户
            if (Grade > MerchantGrade.Registered)
            {
                UpdateGrade(MerchantGrade.Registered);
            }

            // 数据核验轴重置：上一任认领期留下的"已核验"标记与新互联网商户（天然 false）区分，必须清
            IsDataVerified = false;
            LastVerifiedAt = null;
            VerifiedBy = null;
            DataRemark = null;

            // 认领人覆写的联系方式与官网清空（隐私+误导止血，互联网数据管线刷新前保持空白）
            Contact = new ContactInfo();
            Website = null;

            // 商品关联由 helper ExecuteDelete 清理（绕过导航），计数直接归零
            ProductCount = 0;

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
            // v2.15.0 状态统一：在售数 = 审核通过且出售中
            ProductCount = _merchantBearings.Count(mb => mb.ReviewStatus == ListingReviewStatus.Approved && mb.SaleState == SaleState.OnSale);
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

        // ============ 查询方法 ============

        /// <summary>
        /// 获取商家摘要
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string> { Name };
            if (IsVerified) parts.Add("[已认证]");
            // 改动说明（v2.13.0）：展示名已自带"商家"后缀，原拼接产生"入驻商家商家"，去掉多余后缀
            parts.Add(GetGradeDisplayName());
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
            // 改动说明（v2.18.0 卡级重构）：展示名改为卡级口径（注册商户/铜卡/银卡/金卡），
            // 认证不再是档位名（认证徽章由 IsVerified 独立展示）
            MerchantGrade.Registered => "注册商户",
            MerchantGrade.Bronze => "铜卡",
            MerchantGrade.Silver => "银卡",
            MerchantGrade.Gold => "金卡",
            _ => "未定级"
        };
    }
}

using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商家 - 聚合根
    /// 代表在平台上销售轴承产品的企业实体，可以是生产厂家、经销商、贸易商等
    /// 对应接口：GET /api/merchants/search、GET /api/merchants/{id} 等
    /// </summary>
    public class Merchant : BaseEntity
    {
        /// <summary>
        /// 商家名称（展示用）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 公司全称（营业执照上的正式名称）
        /// </summary>
        public string? CompanyName { get; private set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public MerchantType Type { get; private set; }

        /// <summary>
        /// 联系方式（值对象）
        /// 包含联系人、电话、手机、邮箱、地址等公开联系信息
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
        /// 是否已通过平台认证
        /// </summary>
        public bool IsVerified { get; private set; }

        /// <summary>
        /// 认证通过时间
        /// </summary>
        public DateTime? VerifiedAt { get; private set; }

        /// <summary>
        /// 商家等级（铂金、金牌、银牌、普通）
        /// 用于搜索结果排序、权益区分等
        /// </summary>
        public MerchantGrade Grade { get; private set; }

        // ============ 导航属性 ============

        /// <summary>
        /// 员工列表
        /// 该商家的所有注册员工账号
        /// </summary>
        public List<User> Staff { get; private set; } = [];

        /// <summary>
        /// 产品目录
        /// 该商家销售的所有产品关联
        /// </summary>
        public List<MerchantBearing> MerchantBearings { get; private set; } = [];

        /// <summary>
        /// 关注此商家的用户
        /// </summary>
        public List<UserFollow> FollowedByUsers { get; private set; } = [];

        /// <summary>
        /// 关注者数量
        /// </summary>
        public int FollowerCount => FollowedByUsers.Count;

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Merchant() { }

        /// <summary>
        /// 创建新商家
        /// </summary>
        /// <param name="name">商家名称</param>
        /// <param name="type">商家类型</param>
        /// <param name="contact">联系方式</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        /// <exception cref="ArgumentNullException">contact为空时抛出</exception>
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
            Grade = MerchantGrade.Regular;
        }

        /// <summary>
        /// 更新基本信息
        /// </summary>
        /// <param name="companyName">公司全称</param>
        /// <param name="description">商家简介</param>
        /// <param name="businessScope">经营范围</param>
        public void UpdateBasicInfo(
            string? companyName,
            string? description,
            string? businessScope)
        {
            CompanyName = companyName;
            Description = description;
            BusinessScope = businessScope;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新联系方式
        /// </summary>
        /// <param name="newContact">新的联系方式值对象</param>
        /// <exception cref="ArgumentNullException">newContact为空时抛出</exception>
        public void UpdateContact(ContactInfo newContact)
        {
            Contact = newContact ?? throw new ArgumentNullException(nameof(newContact));
            UpdateTimestamp();
        }

        /// <summary>
        /// 认证商家
        /// 由平台管理员审核后调用
        /// 对应接口：PUT /api/admin/merchants/{id}/verify
        /// </summary>
        /// <exception cref="InvalidOperationException">商家已经认证时抛出</exception>
        public void Verify()
        {
            if (IsVerified)
                throw new InvalidOperationException("商家已经认证");

            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            UpdateTimestamp();

            // 触发商家认证事件
            AddDomainEvent(new MerchantVerifiedEvent(Id, Name));
        }

        /// <summary>
        /// 取消认证
        /// 由平台管理员调用
        /// </summary>
        /// <exception cref="InvalidOperationException">商家未认证时抛出</exception>
        public void Unverify()
        {
            if (!IsVerified)
                throw new InvalidOperationException("商家未认证");

            IsVerified = false;
            VerifiedAt = null;
            UpdateTimestamp();

            // 触发商家取消认证事件
            AddDomainEvent(new MerchantUnverifiedEvent(Id, Name));
        }

        /// <summary>
        /// 更新商家等级
        /// </summary>
        /// <param name="newGrade">新等级</param>
        public void UpdateGrade(MerchantGrade newGrade)
        {
            var oldGrade = Grade;
            Grade = newGrade;
            UpdateTimestamp();

            // 触发等级变更事件
            AddDomainEvent(new MerchantGradeChangedEvent(Id, oldGrade, newGrade));
        }

        /// <summary>
        /// 添加员工
        /// </summary>
        /// <param name="user">员工用户</param>
        /// <exception cref="ArgumentNullException">user为空时抛出</exception>
        internal void AddStaff(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!Staff.Contains(user))
            {
                Staff.Add(user);
                user.AssignToMerchant(Id);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除员工
        /// </summary>
        /// <param name="user">员工用户</param>
        internal void RemoveStaff(User user)
        {
            if (user != null && Staff.Contains(user))
            {
                Staff.Remove(user);
                user.RemoveFromMerchant();
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 添加产品关联
        /// </summary>
        /// <param name="merchantBearing">商家-轴承关联实体</param>
        /// <exception cref="ArgumentNullException">merchantBearing为空时抛出</exception>
        internal void AddMerchantBearing(MerchantBearing merchantBearing)
        {
            if (merchantBearing == null)
                throw new ArgumentNullException(nameof(merchantBearing));

            if (!MerchantBearings.Any(mb => mb.BearingId == merchantBearing.BearingId))
            {
                MerchantBearings.Add(merchantBearing);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除产品关联
        /// </summary>
        /// <param name="bearingId">轴承ID</param>
        internal void RemoveMerchantBearing(Guid bearingId)
        {
            var mb = MerchantBearings.FirstOrDefault(x => x.BearingId == bearingId);
            if (mb != null)
            {
                MerchantBearings.Remove(mb);
                UpdateTimestamp();
            }
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
    }
}

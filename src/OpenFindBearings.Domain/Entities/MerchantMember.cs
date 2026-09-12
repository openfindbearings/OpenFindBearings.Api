using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商户成员实体（一人多商户的承载）
    /// 记录一个用户属于哪个商户、在该商户的角色，取代 User.MerchantId 单值列的"一人一商户"硬约束
    /// </summary>
    public class MerchantMember : BaseEntity
    {
        /// <summary>
        /// 商户管理员角色名
        /// </summary>
        public const string RoleMerchantAdmin = "MerchantAdmin";

        /// <summary>
        /// 商户员工角色名
        /// </summary>
        public const string RoleMerchantStaff = "MerchantStaff";

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 商户ID
        /// </summary>
        public Guid MerchantId { get; private set; }

        /// <summary>
        /// 商户导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 商户域角色（MerchantAdmin / MerchantStaff）
        /// </summary>
        public string Role { get; private set; }

        /// <summary>
        /// 成员状态（Active / Removed）
        /// </summary>
        public MerchantMemberStatus Status { get; private set; }

        /// <summary>
        /// 邀请人ID（入驻申请人或邀请员工的管理员）
        /// </summary>
        public Guid? InvitedBy { get; private set; }

        /// <summary>
        /// 加入时间
        /// </summary>
        public DateTime JoinedAt { get; private set; }

        /// <summary>
        /// 移除时间
        /// </summary>
        public DateTime? RemovedAt { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private MerchantMember()
        {
            Role = RoleMerchantStaff;
        }

        /// <summary>
        /// 创建商户成员
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="merchantId">商户ID</param>
        /// <param name="role">商户域角色（MerchantAdmin/MerchantStaff）</param>
        /// <param name="invitedBy">邀请人ID（可选）</param>
        /// <exception cref="ArgumentException">参数校验异常</exception>
        public MerchantMember(
            Guid userId,
            Guid merchantId,
            string role,
            Guid? invitedBy = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (merchantId == Guid.Empty)
                throw new ArgumentException("商户ID不能为空", nameof(merchantId));
            if (role != RoleMerchantAdmin && role != RoleMerchantStaff)
                throw new ArgumentException("商户域角色必须为 MerchantAdmin 或 MerchantStaff", nameof(role));

            UserId = userId;
            MerchantId = merchantId;
            Role = role;
            Status = MerchantMemberStatus.Active;
            InvitedBy = invitedBy;
            JoinedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 是否商户管理员
        /// </summary>
        public bool IsAdmin => Role == RoleMerchantAdmin;

        /// <summary>
        /// 重新入伙（复用 Removed 行，不新增第二行）
        /// </summary>
        /// <param name="role">新角色（可选，缺省保留原角色）</param>
        /// <param name="invitedBy">新邀请人（可选）</param>
        public void Rejoin(string? role = null, Guid? invitedBy = null)
        {
            if (!string.IsNullOrWhiteSpace(role))
                Role = role;
            Status = MerchantMemberStatus.Active;
            InvitedBy = invitedBy;
            RemovedAt = null;
            JoinedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 变更角色
        /// </summary>
        public void ChangeRole(string role)
        {
            if (role != RoleMerchantAdmin && role != RoleMerchantStaff)
                throw new ArgumentException("商户域角色必须为 MerchantAdmin 或 MerchantStaff", nameof(role));

            Role = role;
            UpdateTimestamp();
        }

        /// <summary>
        /// 移除成员（软移除，保留审计信息）
        /// </summary>
        public void Remove()
        {
            Status = MerchantMemberStatus.Removed;
            RemovedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 停用成员（离职/异常处置用，保留成员关系但立即失去操作权限，可恢复）
        /// </summary>
        public void Suspend()
        {
            Status = MerchantMemberStatus.Suspended;
            UpdateTimestamp();
        }

        /// <summary>
        /// 恢复被停用的成员
        /// </summary>
        public void Reactivate()
        {
            Status = MerchantMemberStatus.Active;
            RemovedAt = null;
            JoinedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}

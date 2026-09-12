using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 员工邀请记录
    /// </summary>
    public class StaffInvitation : BaseEntity
    {
        public Guid MerchantId { get; private set; }
        public string? Email { get; private set; }
        public string? Phone { get; private set; }
        public string? Role { get; private set; }
        public string InvitationCode { get; private set; } = string.Empty;
        public Guid OperatorId { get; private set; }

        /// <summary>
        /// 邀请类型（员工邀请 / 管理员提名）
        /// </summary>
        public InvitationType Type { get; private set; }

        /// <summary>
        /// 邀请状态（待处理/已接受/已拒绝/已过期/已撤销）
        /// </summary>
        public InvitationStatus Status { get; private set; }

        /// <summary>
        /// 提名时发起人是否默认入伙为员工
        /// </summary>
        public bool InitiatorJoins { get; private set; }

        public bool IsCompleted { get; private set; }
        public string? CompletedSub { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        private StaffInvitation() { }

        public StaffInvitation(
            Guid merchantId,
            string? email,
            string? phone,
            string? role,
            string invitationCode,
            Guid operatorId,
            InvitationType type = InvitationType.Staff,
            InvitationStatus status = InvitationStatus.Pending,
            bool initiatorJoins = true)
        {
            MerchantId = merchantId;
            Email = email;
            Phone = phone;
            Role = role;
            InvitationCode = invitationCode;
            OperatorId = operatorId;
            Type = type;
            Status = status;
            InitiatorJoins = initiatorJoins;
            IsCompleted = false;
        }

        public void Complete(string sub)
        {
            IsCompleted = true;
            Status = InvitationStatus.Accepted;
            CompletedSub = sub;
            CompletedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Decline()
        {
            Status = InvitationStatus.Declined;
            UpdateTimestamp();
        }

        public void Revoke()
        {
            Status = InvitationStatus.Revoked;
            UpdateTimestamp();
        }

        public void MarkExpired()
        {
            Status = InvitationStatus.Expired;
            UpdateTimestamp();
        }

        public bool IsExpired()
        {
            return CreatedAt.AddDays(7) < DateTime.UtcNow;
        }
    }
}

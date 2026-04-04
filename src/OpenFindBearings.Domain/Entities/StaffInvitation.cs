using OpenFindBearings.Domain.Abstractions;

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
            Guid operatorId)
        {
            MerchantId = merchantId;
            Email = email;
            Phone = phone;
            Role = role;
            InvitationCode = invitationCode;
            OperatorId = operatorId;
            IsCompleted = false;
        }

        public void Complete(string sub)
        {
            IsCompleted = true;
            CompletedSub = sub;
            CompletedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public bool IsExpired()
        {
            return CreatedAt.AddDays(7) < DateTime.UtcNow;
        }
    }
}

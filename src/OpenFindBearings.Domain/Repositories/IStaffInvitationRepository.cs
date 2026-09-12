using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IStaffInvitationRepository
    {
        Task<StaffInvitation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取某商户指定类型的最新邀请（提名审核通过时建成员行用）
        /// </summary>
        Task<StaffInvitation?> GetLatestByMerchantAndTypeAsync(Guid merchantId, InvitationType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按手机号查询待接受的提名邀请（G2：被提名人查看"待我接受的提名"，
        /// 手机号取自 JWT phone_number claim，用户只能看到发给自己的邀请）
        /// </summary>
        Task<List<StaffInvitation>> GetPendingNominationsByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    }
}

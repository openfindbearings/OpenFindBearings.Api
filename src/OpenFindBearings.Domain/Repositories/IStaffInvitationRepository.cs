using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IStaffInvitationRepository
    {
        Task<StaffInvitation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按主键获取邀请（v2.9.0 员工邀请接受/拒绝/撤销按 invitationId 定位）
        /// </summary>
        Task<StaffInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取某商户指定类型的最新邀请（提名审核通过时建成员行用）
        /// </summary>
        Task<StaffInvitation?> GetLatestByMerchantAndTypeAsync(Guid merchantId, InvitationType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按手机号查询待接受的提名邀请（G2：被提名人查看"待我接受的提名"，
        /// 手机号取自 JWT phone_number claim，用户只能看到发给自己的邀请）
        /// </summary>
        Task<List<StaffInvitation>> GetPendingNominationsByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按手机号或邮箱查询待确认员工邀请（v2.9.0 邀请确认制：管理员添加已注册用户转为待确认邀请，
        /// 对方登录后按 JWT phone_number/email claim 匹配查看，同意后才建成员行）
        /// </summary>
        Task<List<StaffInvitation>> GetPendingStaffInvitationsByContactAsync(string? phone, string? email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按商户查询待确认员工邀请（v2.9.0 成员列表合并"已邀请"行 + 撤销入口用）
        /// </summary>
        Task<List<StaffInvitation>> GetPendingStaffInvitationsByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量查出被进行中提名锁定的商户ID（v2.9.0 入驻发现搜索：提名 Pending/Accepted 且 7 天内
        /// 的商户不对外开放认领，锁定判定从认领池查询移到标记计算）
        /// </summary>
        Task<HashSet<Guid>> GetNominationLockedMerchantIdsAsync(IEnumerable<Guid> merchantIds, CancellationToken cancellationToken = default);

        Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    }
}

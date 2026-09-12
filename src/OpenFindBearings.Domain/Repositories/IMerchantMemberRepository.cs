using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商户成员仓储接口
    /// 负责 (用户, 商户, 商户域角色) 成员关系的查询与写入，是一人多商户的唯一事实源
    /// </summary>
    public interface IMerchantMemberRepository
    {
        /// <summary>
        /// 按ID获取成员
        /// </summary>
        Task<MerchantMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户在某商户的在职成员关系
        /// </summary>
        Task<MerchantMember?> GetActiveByUserAndMerchantAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户在某商户的成员关系（含已移除行，用于复用 Removed 行重新入伙）
        /// </summary>
        Task<MerchantMember?> GetByUserAndMerchantAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的全部在职成员关系
        /// </summary>
        Task<List<MerchantMember>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商户的全部在职成员
        /// </summary>
        Task<List<MerchantMember>> GetActiveByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商户在职管理员数量
        /// </summary>
        Task<int> CountActiveAdminsAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统计全库在职成员数量（Dashboard 商家员工口径）
        /// </summary>
        Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 新增成员
        /// </summary>
        Task AddAsync(MerchantMember member, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新成员
        /// </summary>
        Task UpdateAsync(MerchantMember member, CancellationToken cancellationToken = default);
    }
}

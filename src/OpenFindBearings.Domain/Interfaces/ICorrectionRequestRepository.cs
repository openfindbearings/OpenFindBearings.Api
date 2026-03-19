using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 纠错请求仓储接口
    /// 对应接口：POST /api/bearings/{id}/corrections、POST /api/merchants/{id}/corrections
    /// </summary>
    public interface ICorrectionRequestRepository
    {
        /// <summary>
        /// 根据ID获取纠错
        /// </summary>
        Task<CorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核纠错列表
        /// </summary>
        Task<List<CorrectionRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据目标类型和目标ID获取纠错列表
        /// </summary>
        Task<List<CorrectionRequest>> GetByTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取用户提交的纠错
        /// </summary>
        Task<List<CorrectionRequest>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加纠错
        /// </summary>
        Task AddAsync(CorrectionRequest correction, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新纠错
        /// </summary>
        Task UpdateAsync(CorrectionRequest correction, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有纠错
        /// </summary>
        Task<List<CorrectionRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}

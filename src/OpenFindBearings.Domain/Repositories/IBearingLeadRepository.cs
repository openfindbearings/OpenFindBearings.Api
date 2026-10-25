using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 型号线索查询参数
    /// </summary>
    public class BearingLeadSearchParams
    {
        /// <summary>
        /// 状态筛选（Pending=待建档 / Ignored=已忽略 / Resolved=已入库；空=全部）
        /// </summary>
        public BearingLeadStatus? Status { get; set; }

        /// <summary>
        /// 型号关键词（模糊匹配型号原文）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 型号线索仓储接口
    /// 负责记录"型号库未收录型号"的出现次数，并提供管理端查询与忽略/恢复
    /// </summary>
    public interface IBearingLeadRepository
    {
        /// <summary>
        /// 记录一次型号线索出现（按型号去重键原子累加，重复型号只增次数）
        /// 使用独立 SQL upsert，不参与业务事务的变更跟踪，避免影响发布/应答主流程
        /// </summary>
        /// <param name="partNumber">型号原文</param>
        /// <param name="brand">期望品牌，可空</param>
        /// <param name="source">出现来源</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task RecordAsync(string partNumber, string? brand, BearingLeadSource source, CancellationToken cancellationToken = default);

        /// <summary>
        /// 判断型号库是否已收录该型号（按去重键忽略大小写比对，含已软删除型号）
        /// 用于记录线索前过滤"其实已有型号"的噪音，并与列表"已入库"判定保持同一口径
        /// </summary>
        /// <param name="partNumberKey">型号去重键（见 BearingLead.NormalizeKey）</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<bool> ExistsInLibraryAsync(string partNumberKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页查询型号线索（返回项的 IsResolved 已按型号库存在性回填）
        /// </summary>
        Task<PagedResult<BearingLead>> SearchAsync(BearingLeadSearchParams searchParams, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按 ID 获取型号线索
        /// </summary>
        Task<BearingLead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新型号线索（仅标记为已修改，提交由 UnitOfWork 负责）
        /// </summary>
        Task UpdateAsync(BearingLead lead, CancellationToken cancellationToken = default);
    }
}

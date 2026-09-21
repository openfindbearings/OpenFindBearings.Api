using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 站内信仓储接口：收件人维度的写入、分页读取、未读计数与已读标记
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>新增一条站内信</summary>
        Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>标记变更（仅 Modified 判定，提交由 UnitOfWork 管道完成）</summary>
        Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>按 ID 读取（限本人，防越权标记他人通知）</summary>
        Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>收件人分页列表（按时间倒序），unreadOnly 只看未读</summary>
        Task<(List<Notification> Items, int Total)> GetByUserAsync(Guid userId, bool unreadOnly,
            int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>未读条数（角标用）</summary>
        Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>把收件人全部未读批量置已读，返回影响行数</summary>
        Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按"类型+业务对象"把收件人未读通知置已读，返回影响行数。
        /// 改动说明（v2.11.0）：邀请被接受/拒绝时服务端自动核销对应邀请站内信，
        ///   否则角标计数永远包含已处理邀请的残留未读。
        /// </summary>
        Task<int> MarkReadByTypeAsync(Guid userId, string type, Guid? bizId,
            CancellationToken cancellationToken = default);
    }
}

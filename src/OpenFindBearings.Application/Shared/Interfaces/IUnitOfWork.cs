using MediatR;

namespace OpenFindBearings.Application.Shared.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>
        /// 保存所有更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有领域事件（使用 MediatR 的 INotification）
        /// </summary>
        IReadOnlyList<INotification> GetDomainEvents();

        /// <summary>
        /// 清空领域事件
        /// </summary>
        void ClearDomainEvents();
    }
}
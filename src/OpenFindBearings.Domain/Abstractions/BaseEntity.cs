using MediatR;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenFindBearings.Domain.Abstractions
{
    /// <summary>
    /// 领域实体基类
    /// 所有实体都继承自此类，提供通用属性和行为
    /// </summary>
    public abstract class BaseEntity
    {
        private readonly List<INotification> _domainEvents = new();

        /// <summary>
        /// 实体唯一标识（主键）
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// 创建时间（UTC）
        /// </summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// 最后更新时间（UTC）
        /// </summary>
        public DateTime? UpdatedAt { get; protected set; }

        /// <summary>
        /// 是否有效（软删除标记）
        /// </summary>
        public bool IsActive { get; protected set; } = true;

        /// <summary>
        /// 领域事件集合
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// 默认构造函数，供EF Core使用
        /// </summary>
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新时间戳
        /// 实体被修改时调用
        /// </summary>
        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

        /// <summary>
        /// 软删除
        /// </summary>
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// 恢复软删除
        /// </summary>
        public void Activate() => IsActive = true;

        /// <summary>
        /// 添加领域事件
        /// </summary>
        protected void AddDomainEvent(INotification domainEvent) => _domainEvents.Add(domainEvent);

        /// <summary>
        /// 移除领域事件
        /// </summary>
        protected void RemoveDomainEvent(INotification domainEvent) => _domainEvents.Remove(domainEvent);

        /// <summary>
        /// 清空领域事件
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}

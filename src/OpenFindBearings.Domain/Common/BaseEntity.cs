namespace OpenFindBearings.Domain.Common
{
    /// <summary>
    /// 所有领域实体的基类
    /// 提供通用属性和行为
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 实体唯一标识
        /// 使用Guid保证全局唯一性
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// 创建时间（UTC）
        /// 记录实体首次持久化的时间
        /// </summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// 最后更新时间（UTC）
        /// 记录实体最后一次修改的时间
        /// </summary>
        public DateTime? UpdatedAt { get; protected set; }

        /// <summary>
        /// 是否有效（软删除标记）
        /// true: 有效数据，false: 已删除（软删除）
        /// </summary>
        public bool IsActive { get; protected set; } = true;

        /// <summary>
        /// 默认构造函数，供EF Core使用
        /// 自动生成Id和创建时间
        /// </summary>
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新时间戳
        /// 每次修改实体时调用
        /// </summary>
        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

        /// <summary>
        /// 软删除
        /// 将IsActive标记为false
        /// </summary>
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// 恢复软删除
        /// 将IsActive标记为true
        /// </summary>
        public void Activate() => IsActive = true;

        /// <summary>
        /// 判断两个实体是否相等
        /// 实体相等意味着Id相同
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not BaseEntity other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            return Id == other.Id;
        }

        /// <summary>
        /// 获取实体哈希码
        /// 基于Id生成
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// 重载 == 运算符
        /// </summary>
        public static bool operator ==(BaseEntity? left, BaseEntity? right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// 重载 != 运算符
        /// </summary>
        public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
    }
}

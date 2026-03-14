namespace OpenFindBearings.Domain.Common
{
    /// <summary>
    /// 所有值对象的基类
    /// 值对象没有唯一标识，通过其属性值判断相等性
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// 获取用于比较的组件列表
        /// 子类必须实现此方法，返回所有参与相等性比较的属性
        /// </summary>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// 判断两个值对象是否相等
        /// 比较所有属性值是否完全相同
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// 获取哈希码
        /// 基于所有属性值的哈希码组合而成
        /// </summary>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 重载 == 运算符
        /// </summary>
        public static bool operator ==(ValueObject? left, ValueObject? right)
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
        public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
    }
}

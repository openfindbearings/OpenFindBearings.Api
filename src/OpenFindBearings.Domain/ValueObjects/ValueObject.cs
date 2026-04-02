namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 值对象基类
    /// 值对象没有唯一标识，通过其属性值判断相等性
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// 获取用于相等性比较的组件
        /// 子类必须实现此方法
        /// </summary>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// 判断两个值对象是否相等
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

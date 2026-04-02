namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 尺寸参数值对象
    /// 封装轴承的核心尺寸信息
    /// </summary>
    public class Dimensions : ValueObject
    {
        /// <summary>
        /// 内径 (mm)
        /// </summary>
        public decimal InnerDiameter { get; }

        /// <summary>
        /// 外径 (mm)
        /// </summary>
        public decimal OuterDiameter { get; }

        /// <summary>
        /// 宽度 (mm)
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private Dimensions() { }

        /// <summary>
        /// 创建尺寸参数值对象
        /// </summary>
        /// <param name="innerDiameter">内径，必须大于0</param>
        /// <param name="outerDiameter">外径，必须大于内径</param>
        /// <param name="width">宽度，必须大于0</param>
        /// <exception cref="ArgumentException">当尺寸参数不符合业务规则时抛出</exception>
        public Dimensions(decimal innerDiameter, decimal outerDiameter, decimal width)
        {
            if (innerDiameter <= 0)
                throw new ArgumentException("内径必须大于0", nameof(innerDiameter));
            if (outerDiameter <= innerDiameter)
                throw new ArgumentException("外径必须大于内径", nameof(outerDiameter));
            if (width <= 0)
                throw new ArgumentException("宽度必须大于0", nameof(width));

            InnerDiameter = innerDiameter;
            OuterDiameter = outerDiameter;
            Width = width;
        }

        /// <summary>
        /// 获取用于相等性比较的组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return InnerDiameter;
            yield return OuterDiameter;
            yield return Width;
        }

        /// <summary>
        /// 返回尺寸的字符串表示
        /// </summary>
        public override string ToString() => $"{InnerDiameter}×{OuterDiameter}×{Width}";
    }
}

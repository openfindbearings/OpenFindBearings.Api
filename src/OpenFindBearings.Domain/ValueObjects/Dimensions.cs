using OpenFindBearings.Domain.Abstractions;

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
        public decimal InnerDiameter { get; private set; }

        /// <summary>
        /// 外径 (mm)
        /// </summary>
        public decimal OuterDiameter { get; private set; }

        /// <summary>
        /// 宽度 (mm)
        /// </summary>
        public decimal Width { get; private set; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private Dimensions() { }

        /// <summary>
        /// 创建尺寸参数值对象
        /// </summary>
        public Dimensions(decimal innerDiameter, decimal outerDiameter, decimal width)
        {
            if (innerDiameter < 0)
                throw new ArgumentException("内径不能为负数", nameof(innerDiameter));

            if (outerDiameter < innerDiameter)
                throw new ArgumentException("外径不能小于内径", nameof(outerDiameter));

            if (width < 0)
                throw new ArgumentException("宽度不能为负数", nameof(width));

            InnerDiameter = innerDiameter;
            OuterDiameter = outerDiameter;
            Width = width;
        }

        /// <summary>
        /// 是否为完整尺寸（所有尺寸都已指定）
        /// </summary>
        public bool IsComplete => InnerDiameter > 0 && OuterDiameter > 0 && Width > 0;

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

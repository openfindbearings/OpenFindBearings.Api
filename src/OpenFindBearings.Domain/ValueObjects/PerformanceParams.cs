using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 性能参数值对象
    /// 封装轴承的力学性能指标
    /// </summary>
    public class PerformanceParams : ValueObject
    {
        /// <summary>
        /// 动载荷 (kN)
        /// </summary>
        public decimal? DynamicLoadRating { get; }

        /// <summary>
        /// 静载荷 (kN)
        /// </summary>
        public decimal? StaticLoadRating { get; }

        /// <summary>
        /// 极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeed { get; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private PerformanceParams() { }

        /// <summary>
        /// 创建性能参数值对象
        /// </summary>
        /// <param name="dynamicLoad">动载荷</param>
        /// <param name="staticLoad">静载荷</param>
        /// <param name="speed">极限转速</param>
        /// <exception cref="ArgumentException">当动载荷大于静载荷时抛出</exception>
        public PerformanceParams(decimal? dynamicLoad, decimal? staticLoad, decimal? speed)
        {
            if (dynamicLoad.HasValue && staticLoad.HasValue && dynamicLoad > staticLoad)
                throw new ArgumentException("动载荷不能大于静载荷");

            DynamicLoadRating = dynamicLoad;
            StaticLoadRating = staticLoad;
            LimitingSpeed = speed;
        }

        /// <summary>
        /// 获取用于相等性比较的组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return DynamicLoadRating ?? 0;
            yield return StaticLoadRating ?? 0;
            yield return LimitingSpeed ?? 0;
        }

        /// <summary>
        /// 是否有任何性能数据
        /// </summary>
        public bool HasAnyValue =>
            DynamicLoadRating.HasValue ||
            StaticLoadRating.HasValue ||
            LimitingSpeed.HasValue;
    }
}

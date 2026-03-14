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
        /// 创建性能参数
        /// </summary>
        /// <param name="dynamicLoad">动载荷</param>
        /// <param name="staticLoad">静载荷</param>
        /// <param name="speed">极限转速</param>
        /// <exception cref="ArgumentException">当性能参数不符合业务规则时抛出</exception>
        public PerformanceParams(decimal? dynamicLoad, decimal? staticLoad, decimal? speed)
        {
            // 如果两者都有值，检查动载荷是否小于静载荷
            if (dynamicLoad.HasValue && staticLoad.HasValue && dynamicLoad > staticLoad)
                throw new ArgumentException("动载荷不能大于静载荷");

            DynamicLoadRating = dynamicLoad;
            StaticLoadRating = staticLoad;
            LimitingSpeed = speed;
        }

        /// <summary>
        /// 获取用于比较的组件列表
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return DynamicLoadRating;
            yield return StaticLoadRating;
            yield return LimitingSpeed;
        }
    }
}

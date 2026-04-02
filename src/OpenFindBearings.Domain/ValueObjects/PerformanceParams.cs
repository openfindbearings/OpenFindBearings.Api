namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 性能参数值对象
    /// 封装轴承的力学性能指标
    /// </summary>
    public class PerformanceParams : ValueObject
    {
        /// <summary>
        /// 是否存在性能数据（用于EF Core表共享识别）
        /// </summary>
        public bool HasData { get; private set; }

        /// <summary>
        /// 动载荷 (kN)
        /// </summary>
        public decimal? DynamicLoadRating { get; private set; }

        /// <summary>
        /// 静载荷 (kN)
        /// </summary>
        public decimal? StaticLoadRating { get; private set; }

        /// <summary>
        /// 极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeed { get; private set; }

        /// <summary>
        /// 私有构造函数，供EF Core使用
        /// </summary>
        private PerformanceParams()
        {
            HasData = false;
        }

        /// <summary>
        /// 创建性能参数值对象
        /// </summary>
        public PerformanceParams(decimal? dynamicLoad, decimal? staticLoad, decimal? speed)
        {
            if (dynamicLoad.HasValue && staticLoad.HasValue && dynamicLoad > staticLoad)
                throw new ArgumentException("动载荷不能大于静载荷");

            DynamicLoadRating = dynamicLoad;
            StaticLoadRating = staticLoad;
            LimitingSpeed = speed;
            HasData = true;
        }

        /// <summary>
        /// 获取用于相等性比较的组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return HasData;
            yield return DynamicLoadRating ?? 0;
            yield return StaticLoadRating ?? 0;
            yield return LimitingSpeed ?? 0;
        }

        /// <summary>
        /// 是否有任何性能数据
        /// </summary>
        public bool HasAnyValue => HasData;
    }
}

using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.ValueObjects
{
    /// <summary>
    /// 性能参数值对象
    /// 封装轴承的力学性能指标
    /// </summary>
    public class PerformanceParams : ValueObject
    {
        /// <summary>
        /// 是否存在性能数据
        /// </summary>
        public bool HasData { get; private set; }

        /// <summary>
        /// 动载荷 (kN)
        /// </summary>
        public decimal? DynamicLoad { get; private set; }

        /// <summary>
        /// 静载荷 (kN)
        /// </summary>
        public decimal? StaticLoad { get; private set; }

        /// <summary>
        /// 极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeed { get; private set; }

        /// <summary>
        /// 脂极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeedGrease { get; private set; }

        /// <summary>
        /// 油极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeedOil { get; private set; }

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
        public PerformanceParams(decimal? dynamicLoad, decimal? staticLoad, decimal? speed, decimal? greaseSpeed = null, decimal? oilSpeed = null)
        {
            if (dynamicLoad.HasValue && staticLoad.HasValue && dynamicLoad > staticLoad * 1.5m)
                throw new ArgumentException("动载荷异常大于静载荷，请核对数据");

            DynamicLoad = dynamicLoad;
            StaticLoad = staticLoad;
            LimitingSpeed = speed;
            LimitingSpeedGrease = greaseSpeed;
            LimitingSpeedOil = oilSpeed;
            HasData = dynamicLoad.HasValue || staticLoad.HasValue || speed.HasValue || greaseSpeed.HasValue || oilSpeed.HasValue;
        }

        /// <summary>
        /// 是否有任何性能数据
        /// </summary>
        public bool HasAnyValue => HasData;

        /// <summary>
        /// 获取用于相等性比较的组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return HasData;
            yield return DynamicLoad ?? 0;
            yield return StaticLoad ?? 0;
            yield return LimitingSpeed ?? 0;
            yield return LimitingSpeedGrease ?? 0;
            yield return LimitingSpeedOil ?? 0;
        }

        /// <summary>
        /// 获取性能摘要
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();
            if (DynamicLoad.HasValue) parts.Add($"C={DynamicLoad}kN");
            if (StaticLoad.HasValue) parts.Add($"C0={StaticLoad}kN");
            if (LimitingSpeed.HasValue) parts.Add($"n={LimitingSpeed}rpm");
            if (LimitingSpeedGrease.HasValue) parts.Add($"n(脂)={LimitingSpeedGrease}rpm");
            if (LimitingSpeedOil.HasValue) parts.Add($"n(油)={LimitingSpeedOil}rpm");
            return parts.Count > 0 ? string.Join(", ", parts) : "无性能数据";
        }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        public override string ToString() => GetSummary();
    }
}

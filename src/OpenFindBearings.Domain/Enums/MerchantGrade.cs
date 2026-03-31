namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家等级枚举
    /// 用于搜索结果排序和权益区分
    /// </summary>
    public enum MerchantGrade
    {
        /// <summary>
        /// 未知/未评级
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 标准商家
        /// </summary>
        Standard = 1,
        /// <summary>
        /// 优质商家
        /// </summary>
        Premium = 2,
        /// <summary>
        /// 认证商家
        /// </summary>
        Verified = 3
    }
}

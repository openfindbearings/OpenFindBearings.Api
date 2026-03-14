namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家等级枚举
    /// 用于搜索结果排序和权益区分
    /// </summary>
    public enum MerchantGrade
    {
        /// <summary>
        /// 铂金商家（最高等级）
        /// </summary>
        Platinum = 1,

        /// <summary>
        /// 金牌商家
        /// </summary>
        Gold = 2,

        /// <summary>
        /// 银牌商家
        /// </summary>
        Silver = 3,

        /// <summary>
        /// 普通商家（默认等级）
        /// </summary>
        Regular = 4
    }
}

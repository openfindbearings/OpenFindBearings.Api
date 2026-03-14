namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 品牌档次枚举
    /// </summary>
    public enum BrandLevel
    {
        /// <summary>
        /// 国际一线品牌（如 SKF、FAG、NSK）
        /// </summary>
        InternationalPremium = 1,

        /// <summary>
        /// 国际标准品牌（如 TIMKEN、NTN）
        /// </summary>
        InternationalStandard = 2,

        /// <summary>
        /// 国产一线品牌（如 HRB、ZWZ、LYC）
        /// </summary>
        DomesticPremium = 3,

        /// <summary>
        /// 国产二线品牌
        /// </summary>
        DomesticStandard = 4,

        /// <summary>
        /// 经济型品牌
        /// </summary>
        Economy = 5
    }
}

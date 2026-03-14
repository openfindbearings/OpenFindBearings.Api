namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家类型枚举
    /// </summary>
    public enum MerchantType
    {
        /// <summary>
        /// 生产厂家
        /// 自己生产轴承的工厂
        /// </summary>
        Manufacturer = 1,

        /// <summary>
        /// 授权经销商
        /// 获得品牌官方授权的销售商
        /// </summary>
        AuthorizedDealer = 2,

        /// <summary>
        /// 分销商
        /// 批量采购再分销的中间商
        /// </summary>
        Distributor = 3,

        /// <summary>
        /// 贸易商
        /// 从事轴承贸易的企业
        /// </summary>
        Trader = 4
    }
}

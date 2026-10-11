namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家商品销售状态（v2.15.0 状态统一）：替代原 IsOnSale + IsRestocking 两个互斥 bool，
    /// 使非法组合（同时在售又补货）不可表示。三态互斥：已下架 / 出售中 / 补货中。
    /// </summary>
    public enum SaleState
    {
        /// <summary>已下架（商家主动下架，不展示；审核通过后的下架态）</summary>
        OffShelf = 0,

        /// <summary>出售中（在售，公域展示）</summary>
        OnSale = 1,

        /// <summary>补货中（有该型号但暂无货，公域仍展示带徽标）</summary>
        Restocking = 2
    }
}
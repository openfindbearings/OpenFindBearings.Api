namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商城订单物流状态（v2.15.0 Tier2 类型安全化）：替代原 MallOrder.ShipStatus 裸 int 常量。
    /// 值与原常量保持一致（0~4），DB 列不变。虚拟权益恒 NotApplicable，仅实物礼品有物流态。
    /// </summary>
    public enum MallOrderShipStatus
    {
        /// <summary>无需物流（虚拟权益）</summary>
        NotApplicable = 0,

        /// <summary>待商家发货</summary>
        ToShip = 1,

        /// <summary>已发货·待确认收货</summary>
        Shipped = 2,

        /// <summary>已收货（触发结算）</summary>
        Received = 3,

        /// <summary>已退款</summary>
        Refunded = 4
    }
}
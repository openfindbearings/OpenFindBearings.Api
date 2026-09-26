namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商城商品类别（v2.3.0 商城虚拟权益）：决定兑换后的履约策略。
    /// 一期只实装置顶卡；寻货次数包与实物礼品为预留类别（履约器缺省拒绝，避免半截功能上线）
    /// </summary>
    public enum MallItemCategory
    {
        /// <summary>置顶卡：把商户某件在售商品在型号商家列表中置顶 DurationHours 小时</summary>
        PinCard = 1,

        /// <summary>寻货次数包（预留）：购买额外发布/应答额度，待额度银行设计后实装</summary>
        SourcingPack = 2,

        /// <summary>实物礼品（预留）：商家挂礼+托管结算，M1-c 实装</summary>
        Gift = 3
    }

    /// <summary>
    /// 商城订单状态（v2.3.0）：虚拟权益自动履约，正常路径极短（扣分成功即履约）。
    /// Failed 用于履约异常（目标商品已下架/不属于本人商户）——积分按原路退回由 Refunded 表达
    /// </summary>
    public enum MallOrderStatus
    {
        /// <summary>已创建待履约（同事务内瞬时态）</summary>
        Pending = 0,

        /// <summary>已履约（权益已生效）</summary>
        Fulfilled = 1,

        /// <summary>履约失败（已退分）</summary>
        Failed = 2,

        /// <summary>已退款（Admin 冻结/争议处理原路退分）</summary>
        Refunded = 3
    }
}

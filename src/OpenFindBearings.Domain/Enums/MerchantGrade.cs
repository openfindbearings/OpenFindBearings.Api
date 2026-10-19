namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家卡级枚举（v2.18.0 卡级重构）：与"认证"解耦——认证是商家类型（Merchant.IsVerified，独立徽章），
    /// 卡级只描述经营水平，由 IMerchantGradeService 按"认证门槛 + 公域可见在售件数 + 近窗口期成功撮合次数"重算。
    /// 递增序列：注册商户（未认证）→ 铜卡（已认证）→ 银卡 → 金卡；钻卡位预留，当前不启用。
    /// 改动说明：v2.18.0 起枚举数值调整为单调（与业务推进顺序一致），
    ///   Rank 即 (int)Grade，消除旧"数值非单调、需三元映射"的历史包袱；
    ///   金库累计已退出定级判定（金库是平台激励货币账户，非供货能力，且只增不减与可降级卡级语义冲突）。
    /// </summary>
    public enum MerchantGrade
    {
        /// <summary>
        /// 未知/未定级
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 注册商户（Lv1）：未认证或认证审核中，仅登记入驻、无卡级
        /// </summary>
        Registered = 1,
        /// <summary>
        /// 铜卡（Lv2）：已认证（信任基线，不承诺持续供货）
        /// </summary>
        Bronze = 2,
        /// <summary>
        /// 银卡（Lv3）：已认证 + 在售达阈值 + 近窗口期内有成功撮合
        /// </summary>
        Silver = 3,
        /// <summary>
        /// 金卡（Lv4）：已认证 + 在售达阈值 + 近窗口期内成功撮合达更高阈值
        /// </summary>
        Gold = 4
    }
}

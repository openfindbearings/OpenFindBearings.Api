namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家牌级枚举（v2.19.0 有效期制）：与"认证"解耦——认证是商家类型（Merchant.IsVerified，独立徽章），
    /// 牌级只描述经营水平，由 IMerchantGradeService 按"认证门槛 + 公域可见在售件数 + 本有效期成交次数"重算。
    /// 递增序列：普通（未认证或认证但在售不足）→ 铜牌（已认证）→ 银牌 → 金牌；白金/钻石当前只预留命名，不进枚举。
    /// 改动说明（v2.19.0）：Registered 改名 Ordinary（数值仍为 1），展示名由"卡"改"牌"；
    ///   引入 180 天有效期制，期内成交按周期累计、到期结算续期或降档。
    /// </summary>
    public enum MerchantGrade
    {
        /// <summary>
        /// 未知/未定级
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 普通（Lv1）：未认证，或已认证但在售不足；仅登记入驻，无牌级权益
        /// </summary>
        Ordinary = 1,
        /// <summary>
        /// 铜牌（Lv2）：已认证 + 公域可见在售达阈值（信任基线，无成交要求）
        /// </summary>
        Bronze = 2,
        /// <summary>
        /// 银牌（Lv3）：已认证 + 在售达阈值 + 本有效期成功撮合达银牌阈值
        /// </summary>
        Silver = 3,
        /// <summary>
        /// 金牌（Lv4）：已认证 + 在售达阈值 + 本有效期成功撮合达金牌阈值
        /// </summary>
        Gold = 4
    }
}

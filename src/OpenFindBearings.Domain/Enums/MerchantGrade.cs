namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家等级枚举（v2.5.0 商家经济：与"商家等级"统一为同一把梯子，不另设字段——
    ///   Standard=入驻 / Verified=认证 / Premium=口碑 / Gold=金牌，
    ///   由 IMerchantGradeService 按"认证态+在售数+金库累计"规则重算，
    ///   等级是商家 buff 的唯一输入：成员取全部在职商户中最高的一家享受被动加成）
    /// 注意：数值保留历史映射（Standard=1/Premium=2/Verified=3/Gold=4），推进路径
    ///   为 Standard→Verified→Premium→Gold，与数值大小无关，比较请以规则函数为准
    /// </summary>
    public enum MerchantGrade
    {
        /// <summary>
        /// 未知/未定级
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 入驻商家（Lv1：Active 即得）
        /// </summary>
        Standard = 1,
        /// <summary>
        /// 口碑商家（Lv3：认证 + 在售商品达阈值；v2.13.0 由"活跃供给"改名，四档统一"XX商家"）
        /// </summary>
        Premium = 2,
        /// <summary>
        /// 认证商家（Lv2：证照审核通过）
        /// </summary>
        Verified = 3,
        /// <summary>
        /// 金牌商家（Lv4：认证 + 在售达阈值 + 金库累计入账达阈值）
        /// </summary>
        Gold = 4
    }
}

namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家卡级序数扩展（v2.15.0 引入搜索权重，v2.18.0 卡级重构后收敛）。
    /// 改动说明：v2.18.0 起 MerchantGrade 枚举数值已单调（Registered=1/Bronze=2/Silver=3/Gold=4），
    ///   序数直接等于枚举值，本扩展退化为语义化别名（保留调用点，避免大范围改名）。
    /// </summary>
    public static class MerchantGradeExtensions
    {
        /// <summary>
        /// 将商家卡级映射为可比序数（越大卡级越高）：注册商户=1 / 铜卡=2 / 银卡=3 / 金卡=4 / 未知=0。
        /// 口径须与 Taro `merchantGradeToRank` 保持一致
        /// </summary>
        /// <param name="grade">商家卡级</param>
        /// <returns>可比序数（0-4）</returns>
        public static int Rank(this MerchantGrade grade) => grade == MerchantGrade.Unknown ? 0 : (int)grade;
    }
}

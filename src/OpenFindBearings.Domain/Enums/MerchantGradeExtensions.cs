namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家等级序数扩展（v2.15.0 搜索权重批次）。
    /// 背景：`MerchantGrade` 枚举数值保留历史映射（Standard=1/Premium=2/Verified=3/Gold=4），
    /// 与推进路径（Standard→Verified→Premium→Gold）不一致，直接按数值排序会错序；
    /// 本扩展提供"可比序数"，供应用层内存排序与查询打分使用。
    /// </summary>
    public static class MerchantGradeExtensions
    {
        /// <summary>
        /// 将商家等级映射为可比序数（越大等级越高）：Standard=1 / Verified=2 / Premium=3 / Gold=4 / Unknown=0。
        /// 口径须与 Taro `merchantGradeToRank` 保持一致
        /// </summary>
        /// <param name="grade">商家等级</param>
        /// <returns>可比序数（0-4）</returns>
        public static int Rank(this MerchantGrade grade) => grade switch
        {
            MerchantGrade.Standard => 1,
            MerchantGrade.Verified => 2,
            MerchantGrade.Premium => 3,
            MerchantGrade.Gold => 4,
            _ => 0
        };
    }
}
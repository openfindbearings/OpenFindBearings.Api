namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 型号线索来源
    /// 用于记录"平台型号库未收录的型号"是从哪个业务入口出现的
    /// </summary>
    public enum BearingLeadSource
    {
        /// <summary>
        /// 发布寻货时填写的型号未在平台型号库中
        /// </summary>
        Demand = 0,

        /// <summary>
        /// 商家应答寻货时填写的型号行未在平台型号库中
        /// </summary>
        Response = 1
    }
}

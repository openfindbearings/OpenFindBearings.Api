namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 撮合成交回执结果（v2.18.0 撮合成交链）：需求方在选定商家后回填的洽谈结果。
    /// 只有 Confirmed（谈成）才计入商家卡级的"成功撮合"；其余为负向信号（触发降档评估）；
    /// 不回填 = 中性（不计分、不扣分），避免灌水又不冤枉商家
    /// </summary>
    public enum SourcingDealOutcome
    {
        /// <summary>谈成（唯一计分项）</summary>
        Confirmed = 0,
        /// <summary>没谈成：对方无货（现货不实）</summary>
        NoStock = 1,
        /// <summary>没谈成：临时加价</summary>
        PriceRaise = 2,
        /// <summary>没谈成：拖延（交期/响应）</summary>
        Delayed = 3,
        /// <summary>没谈成：其他原因</summary>
        Other = 4
    }
}

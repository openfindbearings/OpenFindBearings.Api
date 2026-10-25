namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 型号线索状态（计算态，不入库）
    /// 由"是否被忽略"与"平台型号库是否已有同型号"两个事实推导
    /// </summary>
    public enum BearingLeadStatus
    {
        /// <summary>
        /// 待建档：未被忽略，且型号库中仍无该型号
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已忽略：管理员确认无需建档
        /// </summary>
        Ignored = 1,

        /// <summary>
        /// 已入库：型号库中已存在同型号（建档后自动判定，无需人工标记）
        /// </summary>
        Resolved = 2
    }
}

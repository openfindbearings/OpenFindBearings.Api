namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 纠错请求状态枚举
    /// </summary>
    public enum CorrectionStatus
    {
        /// <summary>
        /// 待审核
        /// 新提交的纠错请求，等待管理员处理
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已通过
        /// 管理员审核通过，系统将应用修改
        /// </summary>
        Approved = 1,

        /// <summary>
        /// 已拒绝
        /// 管理员拒绝该纠错请求，不进行修改
        /// </summary>
        Rejected = 2
    }
}

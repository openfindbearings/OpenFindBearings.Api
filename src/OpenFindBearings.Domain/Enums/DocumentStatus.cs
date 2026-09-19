namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商户证照材料审核状态（v2.7.0 由 LicenseVerificationStatus 改名泛化）
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// 待审核
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已通过
        /// </summary>
        Approved = 1,

        /// <summary>
        /// 已拒绝
        /// </summary>
        Rejected = 2
    }
}

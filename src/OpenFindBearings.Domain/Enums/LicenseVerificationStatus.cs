namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 营业执照审核状态
    /// </summary>
    public enum LicenseVerificationStatus
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

namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 邀请状态
    /// </summary>
    public enum InvitationStatus
    {
        /// <summary>
        /// 待处理（已发送，等待受邀人响应）
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已接受（受邀人已响应）
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// 已拒绝（受邀人明确拒绝）
        /// </summary>
        Declined = 2,

        /// <summary>
        /// 已过期（超过有效期未响应）
        /// </summary>
        Expired = 3,

        /// <summary>
        /// 已撤销（邀请人主动撤销）
        /// </summary>
        Revoked = 4
    }
}

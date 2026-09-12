namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商户成员状态
    /// </summary>
    public enum MerchantMemberStatus
    {
        /// <summary>
        /// 在职（当前属于该商户）
        /// </summary>
        Active = 0,

        /// <summary>
        /// 已移除（历史成员，可被重新邀请复用）
        /// </summary>
        Removed = 1,

        /// <summary>
        /// 已停用（保留成员关系但无操作权限，可恢复；用于管理员离职交接、异常处置）
        /// </summary>
        Suspended = 2
    }
}

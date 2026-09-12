namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家状态
    /// </summary>
    public enum MerchantStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Active = 0,

        /// <summary>
        /// 已禁用
        /// </summary>
        Suspended = 1,

        /// <summary>
        /// 待审核
        /// </summary>
        Pending = 2,

        /// <summary>
        /// 草稿（提名他人为管理员时的未生效商户，不可被C端检索、不被Sync合并）
        /// </summary>
        Draft = 3
    }
}

namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 邀请类型
    /// </summary>
    public enum InvitationType
    {
        /// <summary>
        /// 员工邀请（管理员邀请员工加入已生效商户）
        /// </summary>
        Staff = 0,

        /// <summary>
        /// 提名（发起人提名他人为管理员，商户尚在 Draft 阶段）
        /// </summary>
        Nomination = 1
    }
}

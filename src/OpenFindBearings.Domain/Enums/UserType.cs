namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 用户类型
    /// </summary>
    public enum UserType
    {
        /// <summary>
        /// 平台管理员
        /// </summary>
        Admin = 1,
        /// <summary>
        /// 商家员工（属于某个商家）
        /// </summary>
        MerchantStaff = 2,
        /// <summary>
        /// 个人用户（不属于任何商家）
        /// </summary>
        Individual = 3,
        /// <summary>
        /// 游客（未登录）
        /// </summary>
        Guest = 4
    }
}

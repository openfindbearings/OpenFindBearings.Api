namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// 权限检查服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 检查当前用户是否有指定权限
        /// </summary>
        Task<bool> HasPermissionAsync(string permissionName);

        /// <summary>
        /// 检查当前用户是否有指定角色
        /// </summary>
        bool HasRole(string roleName);

        /// <summary>
        /// 检查当前用户是否是指定商家的员工
        /// </summary>
        Task<bool> IsMerchantStaffAsync(Guid merchantId);

        /// <summary>
        /// 检查当前用户是否是商家管理员
        /// </summary>
        bool IsMerchantAdminAsync();

        /// <summary>
        /// 检查当前用户是否有权限操作指定轴承
        /// </summary>
        Task<bool> CanManageBearingAsync(Guid bearingId);
    }
}

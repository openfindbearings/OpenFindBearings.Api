using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Shared.Services
{
    /// <summary>
    /// 权限检查器
    /// 用于 Handler 中的细粒度权限判断
    /// </summary>
    public class PermissionChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public PermissionChecker(
            IUserRepository userRepository,
            IPermissionRepository permissionRepository,
            IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _permissionRepository = permissionRepository;
            _userRoleRepository = userRoleRepository;
        }

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        public async Task<bool> HasPermissionAsync(Guid userId, string permissionName)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // 管理员拥有所有权限
            var isAdmin = await _userRoleRepository.UserHasRoleAsync(userId, "Admin");
            if (isAdmin) return true;

            // 检查用户是否拥有该权限
            return await _permissionRepository.UserHasPermissionAsync(userId, permissionName);
        }

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        public async Task<bool> HasRoleAsync(Guid userId, string roleName)
        {
            return await _userRoleRepository.UserHasRoleAsync(userId, roleName);
        }

        /// <summary>
        /// 检查用户是否是指定商家的员工
        /// </summary>
        public async Task<bool> IsMerchantStaffAsync(Guid userId, Guid merchantId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user?.MerchantId == merchantId;
        }
    }
}

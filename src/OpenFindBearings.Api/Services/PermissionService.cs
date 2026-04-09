using MediatR;
using OpenFindBearings.Application.Queries.Permissions.CheckUserPermission;
using OpenFindBearings.Domain.Repositories;

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

    /// <summary>
    /// 权限检查服务实现
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;  // 添加
        private readonly IMerchantBearingRepository _merchantBearingRepository;  // 添加
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IMediator mediator,
            ICurrentUserService currentUser,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,  // 注入
            IMerchantBearingRepository merchantBearingRepository,  // 注入
            ILogger<PermissionService> logger)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        /// <summary>
        /// 检查当前用户是否有指定权限
        /// </summary>
        public async Task<bool> HasPermissionAsync(string permissionName)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                return false;

            try
            {
                var query = new CheckUserPermissionQuery
                {
                    UserId = _currentUser.UserId.Value,
                    PermissionName = permissionName
                };

                var hasPermission = await _mediator.Send(query);

                _logger.LogDebug("权限检查: UserId={UserId}, Permission={Permission}, Result={Result}",
                    _currentUser.UserId, permissionName, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "权限检查失败: UserId={UserId}, Permission={Permission}",
                    _currentUser.UserId, permissionName);
                return false;
            }
        }

        /// <summary>
        /// 检查当前用户是否有指定角色
        /// </summary>
        public bool HasRole(string roleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return false;

            return user.HasClaim(c => c.Type == "role" && c.Value == roleName);
        }

        /// <summary>
        /// 检查当前用户是否是指定商家的员工
        /// </summary>
        public async Task<bool> IsMerchantStaffAsync(Guid merchantId)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                return false;

            var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value);
            if (user == null)
                return false;

            // 检查用户是否属于该商家
            return user.MerchantId == merchantId;
        }

        /// <summary>
        /// 检查当前用户是否是商家管理员
        /// </summary>
        public bool IsMerchantAdminAsync()
        {
            return HasRole("MerchantAdmin");
        }

        /// <summary>
        /// 检查当前用户是否有权限操作指定轴承
        /// </summary>
        public async Task<bool> CanManageBearingAsync(Guid bearingId)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                return false;

            // 管理员可以管理所有轴承
            if (await HasPermissionAsync("product.manage.all"))
                return true;

            // 获取当前用户
            var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value);
            if (user == null || !user.MerchantId.HasValue)
                return false;

            // 检查该轴承是否属于该商家的产品
            return await _merchantBearingRepository.IsOwnedByMerchantAsync(bearingId, user.MerchantId.Value);
        }
    }
}

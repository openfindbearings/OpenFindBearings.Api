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
        /// 检查当前用户是否是商家管理员（按成员表 + 当前商户上下文判定）
        /// </summary>
        Task<bool> IsMerchantAdmin();

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
        private readonly IMerchantMemberRepository _merchantMemberRepository;  // 成员表（一人多商户唯一事实源）
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IMediator mediator,
            ICurrentUserService currentUser,
            IHttpContextAccessor httpContextAccessor,
            IMerchantMemberRepository merchantMemberRepository,  // 注入成员仓储
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<PermissionService> logger)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _httpContextAccessor = httpContextAccessor;
            _merchantMemberRepository = merchantMemberRepository;
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
        /// 检查当前用户是否是指定商家的员工（按成员表判定，不再依赖 User.MerchantId 单值列）
        /// </summary>
        public async Task<bool> IsMerchantStaffAsync(Guid merchantId)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                return false;

            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                _currentUser.UserId.Value, merchantId);
            return member != null;
        }

        /// <summary>
        /// 检查当前用户是否当前商户管理员（按成员表 + CurrentMerchantId 判定）
        /// 修复原实现依赖 JWT role claim 导致恒为 false 的问题
        /// </summary>
        public async Task<bool> IsMerchantAdmin()
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue || !_currentUser.CurrentMerchantId.HasValue)
                return false;

            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                _currentUser.UserId.Value, _currentUser.CurrentMerchantId.Value);
            return member != null && member.IsAdmin;
        }

        /// <summary>
        /// 检查当前用户是否有权限操作指定轴承（按当前商户上下文判定）
        /// </summary>
        public async Task<bool> CanManageBearingAsync(Guid bearingId)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                return false;

            // 管理员可以管理所有轴承
            if (await HasPermissionAsync("bearing.manage.all"))
                return true;

            // 无当前商户上下文则无权操作
            if (!_currentUser.CurrentMerchantId.HasValue)
                return false;

            // 必须是该商户的在职成员
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                _currentUser.UserId.Value, _currentUser.CurrentMerchantId.Value);
            if (member == null)
                return false;

            // 检查该轴承是否属于该商家的产品
            return await _merchantBearingRepository.IsOwnedByMerchantAsync(bearingId, _currentUser.CurrentMerchantId.Value);
        }
    }
}

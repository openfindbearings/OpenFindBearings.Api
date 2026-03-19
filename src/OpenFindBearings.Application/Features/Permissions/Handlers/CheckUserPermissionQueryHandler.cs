using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Permissions.Handlers
{
    /// <summary>
    /// 检查用户是否有指定权限查询处理器
    /// </summary>
    public class CheckUserPermissionQueryHandler : IRequestHandler<CheckUserPermissionQuery, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CheckUserPermissionQueryHandler> _logger;

        public CheckUserPermissionQueryHandler(
            IUserRepository userRepository,
            ILogger<CheckUserPermissionQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckUserPermissionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("检查用户权限: UserId={UserId}, Permission={PermissionName}",
                request.UserId, request.PermissionName);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            // 管理员拥有所有权限
            if (user.UserType == Domain.Enums.UserType.Admin)
            {
                return true;
            }

            var hasPermission = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Any(rp => rp.Permission.Name == request.PermissionName);

            return hasPermission;
        }
    }
}

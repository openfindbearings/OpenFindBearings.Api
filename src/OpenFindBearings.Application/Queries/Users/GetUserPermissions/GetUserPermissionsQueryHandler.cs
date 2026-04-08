using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Users.GetUserPermissions
{
    /// <summary>
    /// 获取用户权限列表查询处理器
    /// </summary>
    public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, List<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserPermissionsQueryHandler> _logger;

        public GetUserPermissionsQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserPermissionsQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<string>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户权限列表: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return new List<string>();
            }

            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            return permissions;
        }
    }
}

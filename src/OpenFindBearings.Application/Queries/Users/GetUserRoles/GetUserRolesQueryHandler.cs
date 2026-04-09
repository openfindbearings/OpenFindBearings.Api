using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Users.GetUserRoles
{
    /// <summary>
    /// 获取用户角色列表查询处理器
    /// </summary>
    public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, List<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserRolesQueryHandler> _logger;

        public GetUserRolesQueryHandler(
            IUserRepository userRepository,
            ILogger<GetUserRolesQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<string>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户角色列表: UserId={UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return new List<string>();
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            return roles;
        }
    }
}

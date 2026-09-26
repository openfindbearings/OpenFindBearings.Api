using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Users.GetAllPlatformRoles
{
    /// <summary>
    /// 获取全部用户的平台角色映射处理器：
    /// 返回 Identity sub → 角色名列表 的字典（仅含挂了至少一个角色的用户），
    /// 供 Admin 用户列表一次批量合并角色徽章，避免逐行 by-auth N+1
    /// </summary>
    public class GetAllPlatformRolesQueryHandler : IRequestHandler<GetAllPlatformRolesQuery, Dictionary<string, List<string>>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllPlatformRolesQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Dictionary<string, List<string>>> Handle(
            GetAllPlatformRolesQuery request,
            CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetUsersWithRolesAsync(cancellationToken);
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.AuthUserId)) continue;
                result[user.AuthUserId] = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList();
            }
            return result;
        }
    }
}

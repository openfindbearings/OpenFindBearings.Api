using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Roles.GetAllRoles
{
    /// <summary>
    /// 获取所有角色列表查询处理器
    /// </summary>
    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, List<RoleDto>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetAllRolesQueryHandler> _logger;

        public GetAllRolesQueryHandler(
            IRoleRepository roleRepository,
            ILogger<GetAllRolesQueryHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<List<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取所有角色列表");

            var roles = await _roleRepository.GetAllAsync(cancellationToken);

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
                UserCount = r.UserRoles.Count,
                CreatedAt = r.CreatedAt,
                IsSystemRole = r.Name == "GlobalAdmin" || r.Name == "MerchantAdmin" ||
                               r.Name == "MerchantStaff" || r.Name == "Customer"
            }).ToList();
        }
    }
}

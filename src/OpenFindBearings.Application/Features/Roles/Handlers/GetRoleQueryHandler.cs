using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.DTOs;
using OpenFindBearings.Application.Features.Roles.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 获取单个角色查询处理器
    /// </summary>
    public class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, RoleDto?>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetRoleQueryHandler> _logger;

        public GetRoleQueryHandler(
            IRoleRepository roleRepository,
            ILogger<GetRoleQueryHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<RoleDto?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取角色详情: {RoleId}", request.Id);

            var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
            if (role == null)
            {
                return null;
            }

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
                UserCount = role.UserRoles.Count,
                CreatedAt = role.CreatedAt,
                IsSystemRole = role.Name == "GlobalAdmin" || role.Name == "MerchantAdmin" ||
                               role.Name == "MerchantStaff" || role.Name == "Customer"
            };
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.DTOs;
using OpenFindBearings.Application.Features.Roles.DTOs;
using OpenFindBearings.Application.Features.Roles.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    public class GetRoleDetailQueryHandler : IRequestHandler<GetRoleDetailQuery, RoleDetailDto?>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetRoleDetailQueryHandler> _logger;

        public GetRoleDetailQueryHandler(
            IRoleRepository roleRepository,
            ILogger<GetRoleDetailQueryHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<RoleDetailDto?> Handle(GetRoleDetailQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null) return null;

            return new RoleDetailDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Group = rp.Permission.Name.Split('.').FirstOrDefault(),
                    CreatedAt = rp.Permission.CreatedAt
                }).ToList(),
                UserCount = role.UserRoles.Count,
                CreatedAt = role.CreatedAt,
                IsSystemRole = role.Name == "GlobalAdmin" || role.Name == "MerchantAdmin" ||
                               role.Name == "MerchantStaff" || role.Name == "Customer"
            };
        }
    }
}

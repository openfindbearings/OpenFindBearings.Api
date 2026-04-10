using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Roles.GetRoleDetail
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

            var roleDto = (RoleDetailDto)role.ToDto();
            roleDto.Permissions = role.RolePermissions.Select(rp => rp.Permission.ToDto()).ToList();
            return roleDto;
        }
    }
}

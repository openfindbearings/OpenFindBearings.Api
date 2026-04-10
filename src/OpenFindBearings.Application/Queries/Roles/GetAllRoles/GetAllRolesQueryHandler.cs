using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
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

            return roles.Select(r => r.ToDto()).ToList();
        }
    }
}

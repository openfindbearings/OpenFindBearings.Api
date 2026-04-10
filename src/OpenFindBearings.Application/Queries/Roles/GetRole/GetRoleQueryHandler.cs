using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Queries
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

            return role.ToDto();
        }
    }
}

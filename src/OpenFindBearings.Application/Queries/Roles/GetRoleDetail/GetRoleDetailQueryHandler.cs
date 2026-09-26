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

        /// <summary>
        /// 组装角色详情：RoleDetailDto 直接映射（继承 RoleDto 全字段）
        /// 改动说明（v1.38.0）：原实现 `(RoleDetailDto)role.ToDto()` 把基类实例强转派生类
        /// 必抛 InvalidCastException（角色详情端点隐性 500），改为显式构造 RoleDetailDto
        /// </summary>
        public async Task<RoleDetailDto?> Handle(GetRoleDetailQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null) return null;

            var baseDto = role.ToDto();
            return new RoleDetailDto
            {
                Id = baseDto.Id,
                Name = baseDto.Name,
                DisplayName = baseDto.DisplayName,
                Description = baseDto.Description,
                UserCount = baseDto.UserCount,
                CreatedAt = baseDto.CreatedAt,
                IsSystemRole = baseDto.IsSystemRole,
                Permissions = role.RolePermissions.Select(rp => rp.Permission!.ToDto()).ToList()
            };
        }
    }
}

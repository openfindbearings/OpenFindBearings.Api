using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.DTOs;
using OpenFindBearings.Application.Features.Permissions.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Permissions.Handlers
{
    /// <summary>
    /// 获取所有权限列表查询处理器
    /// </summary>
    public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, List<PermissionDto>>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<GetAllPermissionsQueryHandler> _logger;

        public GetAllPermissionsQueryHandler(
            IPermissionRepository permissionRepository,
            ILogger<GetAllPermissionsQueryHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<List<PermissionDto>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取所有权限列表");

            var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

            // 按权限名分组（便于前端展示）
            var grouped = permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Group = p.Name.Split('.').FirstOrDefault() ?? "其他",
                    CreatedAt = p.CreatedAt
                })
                .OrderBy(p => p.Group)
                .ThenBy(p => p.Name)
                .ToList();

            return grouped;
        }
    }
}

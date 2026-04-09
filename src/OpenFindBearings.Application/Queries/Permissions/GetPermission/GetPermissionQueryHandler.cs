using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Permissions.GetPermission
{
    /// <summary>
    /// 获取单个权限查询处理器
    /// </summary>
    public class GetPermissionQueryHandler : IRequestHandler<GetPermissionQuery, PermissionDto?>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<GetPermissionQueryHandler> _logger;

        public GetPermissionQueryHandler(
            IPermissionRepository permissionRepository,
            ILogger<GetPermissionQueryHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<PermissionDto?> Handle(GetPermissionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取权限详情: {PermissionId}", request.Id);

            var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (permission == null)
            {
                return null;
            }

            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Group = permission.Name.Split('.').FirstOrDefault() ?? "其他",
                CreatedAt = permission.CreatedAt
            };
        }
    }
}

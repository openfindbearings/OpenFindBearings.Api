using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Permissions.DTOs;
using OpenFindBearings.Application.Features.Permissions.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Permissions.Handlers
{
    /// <summary>
    /// 获取权限列表（分页）查询处理器
    /// </summary>
    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, PagedResult<PermissionDto>>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<GetPermissionsQueryHandler> _logger;

        public GetPermissionsQueryHandler(
            IPermissionRepository permissionRepository,
            ILogger<GetPermissionsQueryHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取权限列表: Page={Page}, PageSize={PageSize}, Group={Group}",
                request.Page, request.PageSize, request.Group);

            var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);

            // 按分组过滤
            var filtered = allPermissions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                filtered = filtered.Where(p => p.Name.StartsWith(request.Group + ".", StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = filtered.Count();
            var items = filtered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Group = p.Name.Split('.').FirstOrDefault() ?? "其他",
                    CreatedAt = p.CreatedAt
                })
                .ToList();

            return new PagedResult<PermissionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

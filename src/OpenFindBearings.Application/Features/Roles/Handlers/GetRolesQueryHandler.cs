using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Roles.DTOs;
using OpenFindBearings.Application.Features.Roles.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Roles.Handlers
{
    /// <summary>
    /// 获取角色列表（分页）查询处理器
    /// </summary>
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, PagedResult<RoleDto>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetRolesQueryHandler> _logger;

        public GetRolesQueryHandler(
            IRoleRepository roleRepository,
            ILogger<GetRolesQueryHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<PagedResult<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取角色列表: Page={Page}, PageSize={PageSize}, Keyword={Keyword}",
                request.Page, request.PageSize, request.Keyword);

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);

            // 关键词过滤
            var filtered = allRoles.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                filtered = filtered.Where(r => r.Name.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = filtered.Count();
            var items = filtered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList(),
                    UserCount = r.UserRoles.Count,
                    CreatedAt = r.CreatedAt,
                    IsSystemRole = IsSystemRole(r.Name)
                })
                .ToList();

            return new PagedResult<RoleDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        private bool IsSystemRole(string roleName)
        {
            return roleName == "GlobalAdmin" ||
                   roleName == "MerchantAdmin" ||
                   roleName == "MerchantStaff" ||
                   roleName == "Customer";
        }
    }
}

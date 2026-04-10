using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class RoleExtensions
    {
        public static RoleDto ToDto(this Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => rp.Permission?.Name ?? string.Empty).ToList(),
                UserCount = role.UserRoles?.Count ?? 0,
                CreatedAt = role.CreatedAt,
                IsSystemRole = role.IsSystem
            };
        }
    }
}
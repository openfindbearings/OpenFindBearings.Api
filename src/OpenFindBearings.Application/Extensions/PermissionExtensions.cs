using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class PermissionExtensions
    {
        public static PermissionDto ToDto(this Permission permission)
        {
            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                CreatedAt = permission.CreatedAt
            };
        }
    }
}
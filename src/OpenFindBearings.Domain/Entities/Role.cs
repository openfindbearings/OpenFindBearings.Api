using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;  // 如 "MerchantAdmin", "ProductManager"
        public string? Description { get; set; }

        // 导航属性
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

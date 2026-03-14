using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{  
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;  // 如 "product.edit", "correction.review"
        public string? Description { get; set; }

        // 导航属性
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

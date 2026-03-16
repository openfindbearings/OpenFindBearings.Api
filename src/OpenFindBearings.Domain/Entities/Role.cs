using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 角色实体
    /// 代表系统中的用户角色，用于权限分组
    /// 如 GlobalAdmin（全局管理员）、MerchantAdmin（商家管理员）等
    /// </summary>
    public class Role : BaseEntity
    {
        /// <summary>
        /// 角色名称
        /// 全局唯一，如 "GlobalAdmin", "MerchantAdmin", "MerchantStaff", "Customer"
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 角色描述
        /// 用于说明该角色的权限范围和用途
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 用户-角色关联导航属性
        /// 一个角色可以分配给多个用户
        /// </summary>
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        /// <summary>
        /// 角色-权限关联导航属性
        /// 一个角色可以拥有多个权限
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Role() { }

        /// <summary>
        /// 创建新角色
        /// </summary>
        /// <param name="name">角色名称，如 "GlobalAdmin"</param>
        /// <param name="description">角色描述</param>
        /// <exception cref="ArgumentException">当角色名称为空时抛出</exception>
        public Role(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("角色名称不能为空", nameof(name));

            Name = name;
            Description = description;
        }

        /// <summary>
        /// 更新角色描述
        /// </summary>
        /// <param name="description">新的描述</param>
        public void UpdateDescription(string? description)
        {
            Description = description;
            UpdateTimestamp();
        }
    }
}

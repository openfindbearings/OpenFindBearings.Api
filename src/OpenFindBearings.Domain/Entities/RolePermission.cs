using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 角色-权限关联实体
    /// 建立角色和权限之间的多对多关系
    /// </summary>
    public class RolePermission : BaseEntity
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// 角色导航属性
        /// </summary>
        public Role Role { get; set; } = null!;

        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid PermissionId { get; set; }

        /// <summary>
        /// 权限导航属性
        /// </summary>
        public Permission Permission { get; set; } = null!;

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private RolePermission() { }

        /// <summary>
        /// 创建角色-权限关联
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionId">权限ID</param>
        public RolePermission(Guid roleId, Guid permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}

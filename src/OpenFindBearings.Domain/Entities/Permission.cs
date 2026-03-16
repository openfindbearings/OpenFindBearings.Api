using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 权限实体
    /// 代表系统中的具体操作权限，如 "product.edit", "correction.review" 等
    /// </summary>
    public class Permission : BaseEntity
    {
        /// <summary>
        /// 权限名称（如 "product.edit", "correction.review"）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 角色-权限关联导航属性
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Permission() { }

        /// <summary>
        /// 创建新权限
        /// </summary>
        /// <param name="name">权限名称</param>
        /// <param name="description">权限描述</param>
        public Permission(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("权限名称不能为空", nameof(name));

            Name = name;
            Description = description;
        }

        /// <summary>
        /// 更新权限描述
        /// </summary>
        /// <param name="description">新的描述</param>
        public void UpdateDescription(string? description)
        {
            Description = description;
            UpdateTimestamp();
        }
    }
}

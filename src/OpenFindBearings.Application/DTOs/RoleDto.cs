namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 角色DTO
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 角色名称（英文机器标识，鉴权键）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 人读显示名（可中文，v1.38.0），空则界面回退显示 Name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 拥有的权限列表
        /// </summary>
        public List<string> Permissions { get; set; } = new();

        /// <summary>
        /// 用户数量
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 是否系统角色（不可删除）
        /// </summary>
        public bool IsSystemRole { get; set; }
    }
}

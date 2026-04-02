namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户-角色关联实体
    /// 建立用户和角色之间的多对多关系
    /// </summary>
    public class UserRole : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// 角色导航属性
        /// </summary>
        public Role Role { get; set; } = null!;

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private UserRole() { }

        /// <summary>
        /// 创建用户-角色关联
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        public UserRole(Guid userId, Guid roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }
    }
}

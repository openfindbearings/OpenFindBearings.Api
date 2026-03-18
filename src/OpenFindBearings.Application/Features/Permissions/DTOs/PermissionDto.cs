namespace OpenFindBearings.Application.Features.Permissions.DTOs
{
    /// <summary>
    /// 权限DTO
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 权限名称（如 "product.edit"）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 权限分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}

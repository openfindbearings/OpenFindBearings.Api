namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 角色详情DTO（包含权限完整信息）
    /// 继承 RoleDto，新增 Permissions 字段
    /// </summary>
    public class RoleDetailDto : RoleDto
    {
        /// <summary>
        /// 拥有的权限列表（完整信息）
        /// </summary>
        public new List<PermissionDto> Permissions { get; set; } = [];
    }
}

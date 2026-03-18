using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Admin.DTOs
{
    /// <summary>
    /// 用户管理DTO（管理员用）
    /// </summary>
    public class UserManagementDto : UserDto
    {
        /// <summary>
        /// 是否锁定
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 锁定原因
        /// </summary>
        public string? LockReason { get; set; }

        /// <summary>
        /// 最后登录IP
        /// </summary>
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// 登录次数
        /// </summary>
        public int LoginCount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? AdminRemark { get; set; }
    }
}

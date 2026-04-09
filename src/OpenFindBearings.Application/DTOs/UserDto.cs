using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 用户DTO
    /// 用于展示用户信息
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 认证系统用户ID
        /// </summary>
        public string AuthUserId { get; set; } = string.Empty;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; set; }

        /// <summary>
        /// 用户头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public string UserType { get; set; } = string.Empty;

        /// <summary>
        /// 用户职业（采购员/销售员/工程师/其他）
        /// </summary>
        public UserOccupation? Occupation { get; init; }

        /// <summary>
        /// 公司名称
        /// </summary>
        public string? CompanyName { get; init; }

        /// <summary>
        /// 所属行业
        /// </summary>
        public string? Industry { get; init; }

        /// <summary>
        /// 所属商家ID（如果是商家员工）
        /// </summary>
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 所属商家名称
        /// </summary>
        public string? MerchantName { get; set; }

        /// <summary>
        /// 拥有的角色
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// 拥有的权限
        /// </summary>
        public List<string> Permissions { get; set; } = new();

        /// <summary>
        /// 收藏数量
        /// </summary>
        public int FavoriteCount { get; set; }

        /// <summary>
        /// 关注数量
        /// </summary>
        public int FollowCount { get; set; }

        /// <summary>
        /// 提交纠错数量
        /// </summary>
        public int CorrectionCount { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
    }
}

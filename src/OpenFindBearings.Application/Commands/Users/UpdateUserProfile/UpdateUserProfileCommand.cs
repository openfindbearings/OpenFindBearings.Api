using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Users.UpdateUserProfile
{
    /// <summary>
    /// 更新用户资料命令
    /// </summary>
    public record UpdateUserProfileCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; init; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string? Avatar { get; init; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; init; }

        // ============ 用户画像字段 ============

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
    }
}

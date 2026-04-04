using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 更新用户地区偏好命令
    /// </summary>
    public record UpdateUserRegionPreferenceCommand : IRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 省份
        /// </summary>
        public string? Province { get; init; }

        /// <summary>
        /// 城市
        /// </summary>
        public string? City { get; init; }
    }
}

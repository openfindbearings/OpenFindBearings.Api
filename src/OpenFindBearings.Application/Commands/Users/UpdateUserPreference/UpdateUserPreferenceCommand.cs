using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Users.UpdateUserPreference
{
    /// <summary>
    /// 更新用户偏好命令
    /// 用于更新用户的地区偏好、品牌偏好、类型偏好、价格区间、通知偏好等
    /// </summary>
    public record UpdateUserPreferenceCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        // ============ 地区偏好 ============

        /// <summary>
        /// 偏好省份
        /// </summary>
        public string? Province { get; init; }

        /// <summary>
        /// 偏好城市
        /// </summary>
        public string? City { get; init; }

        // ============ 品牌偏好 ============

        /// <summary>
        /// 偏好品牌ID列表
        /// </summary>
        public List<Guid>? BrandIds { get; init; }

        // ============ 类型偏好 ============

        /// <summary>
        /// 偏好轴承类型ID列表
        /// </summary>
        public List<Guid>? BearingTypeIds { get; init; }

        // ============ 价格区间 ============

        /// <summary>
        /// 最小价格
        /// </summary>
        public decimal? MinPrice { get; init; }

        /// <summary>
        /// 最大价格
        /// </summary>
        public decimal? MaxPrice { get; init; }

        // ============ 通知偏好 ============

        /// <summary>
        /// 是否接收邮件通知
        /// </summary>
        public bool? EmailNotificationEnabled { get; init; }

        /// <summary>
        /// 是否接收短信通知
        /// </summary>
        public bool? SmsNotificationEnabled { get; init; }

        /// <summary>
        /// 是否接收微信通知
        /// </summary>
        public bool? WeChatNotificationEnabled { get; init; }
    }
}

using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Users.GetUserPreference
{
    /// <summary>
    /// 读取当前用户通知偏好查询（v1.6.0 通知设置）。
    /// 无偏好行时返回默认口径（短信关、微信/邮件开），与 UserPreference 实体默认值一致。
    /// </summary>
    /// <param name="UserId">用户 ID</param>
    public record GetUserPreferenceQuery(Guid UserId) : IRequest<UserPreferenceDto>;

    /// <summary>通知偏好 DTO（前端"通知设置"开关态）</summary>
    public class UserPreferenceDto
    {
        /// <summary>短信通知是否开启</summary>
        public bool SmsNotificationEnabled { get; set; }

        /// <summary>微信通知是否开启</summary>
        public bool WeChatNotificationEnabled { get; set; }

        /// <summary>邮件通知是否开启</summary>
        public bool EmailNotificationEnabled { get; set; }
    }

    /// <summary>
    /// 读取通知偏好处理器：取偏好行，无则回默认
    /// </summary>
    public class GetUserPreferenceQueryHandler : IRequestHandler<GetUserPreferenceQuery, UserPreferenceDto>
    {
        private readonly IUserPreferenceRepository _preferenceRepository;

        public GetUserPreferenceQueryHandler(IUserPreferenceRepository preferenceRepository)
        {
            _preferenceRepository = preferenceRepository;
        }

        /// <inheritdoc/>
        public async Task<UserPreferenceDto> Handle(GetUserPreferenceQuery request, CancellationToken cancellationToken)
        {
            var pref = await _preferenceRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            return new UserPreferenceDto
            {
                // 默认口径与 UserPreference 构造函数一致：短信默认关、微信/邮件默认开
                SmsNotificationEnabled = pref?.SmsNotificationEnabled ?? false,
                WeChatNotificationEnabled = pref?.WeChatNotificationEnabled ?? true,
                EmailNotificationEnabled = pref?.EmailNotificationEnabled ?? true
            };
        }
    }
}
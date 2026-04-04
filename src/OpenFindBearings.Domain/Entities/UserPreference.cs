using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public class UserPreference : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 偏好品牌ID列表（JSON）
        /// </summary>
        public string? PreferredBrandIds { get; private set; }

        /// <summary>
        /// 偏好轴承类型ID列表（JSON）
        /// </summary>
        public string? PreferredBearingTypeIds { get; private set; }

        /// <summary>
        /// 价格区间偏好（JSON: {min:0, max:1000}）
        /// </summary>
        public string? PriceRangePreference { get; private set; }

        /// <summary>
        /// 是否接收邮件通知
        /// </summary>
        public bool EmailNotificationEnabled { get; private set; } = true;

        /// <summary>
        /// 是否接收短信通知
        /// </summary>
        public bool SmsNotificationEnabled { get; private set; }

        /// <summary>
        /// 是否接收微信通知
        /// </summary>
        public bool WeChatNotificationEnabled { get; private set; } = true;

        private UserPreference() { }

        public UserPreference(Guid userId)
        {
            UserId = userId;
        }

        public void UpdatePreferredBrands(List<Guid> brandIds)
        {
            PreferredBrandIds = System.Text.Json.JsonSerializer.Serialize(brandIds);
            UpdateTimestamp();
        }

        public void UpdatePreferredBearingTypes(List<Guid> typeIds)
        {
            PreferredBearingTypeIds = System.Text.Json.JsonSerializer.Serialize(typeIds);
            UpdateTimestamp();
        }
    }
}

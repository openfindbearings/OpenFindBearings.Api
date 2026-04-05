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
        /// 偏好省份
        /// </summary>
        public string? PreferredProvince { get; private set; }

        /// <summary>
        /// 偏好城市
        /// </summary>
        public string? PreferredCity { get; private set; }

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

        /// <summary>
        /// 创建用户偏好设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        public UserPreference(Guid userId)
        {
            UserId = userId;
        }

        /// <summary>
        /// 更新偏好品牌列表
        /// </summary>
        /// <param name="brandIds">品牌ID列表</param>
        public void UpdatePreferredBrands(List<Guid> brandIds)
        {
            PreferredBrandIds = System.Text.Json.JsonSerializer.Serialize(brandIds);
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新偏好轴承类型列表
        /// </summary>
        /// <param name="typeIds">轴承类型ID列表</param>
        public void UpdatePreferredBearingTypes(List<Guid> typeIds)
        {
            PreferredBearingTypeIds = System.Text.Json.JsonSerializer.Serialize(typeIds);
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新地区偏好
        /// </summary>
        /// <param name="province">省份</param>
        /// <param name="city">城市</param>
        public void UpdateRegionPreference(string? province, string? city)
        {
            PreferredProvince = province;
            PreferredCity = city;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新价格区间偏好
        /// </summary>
        /// <param name="minPrice">最低价格</param>
        /// <param name="maxPrice">最高价格</param>
        public void UpdatePriceRange(decimal? minPrice, decimal? maxPrice)
        {
            var priceRange = new { Min = minPrice, Max = maxPrice };
            PriceRangePreference = System.Text.Json.JsonSerializer.Serialize(priceRange);
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新通知偏好
        /// </summary>
        /// <param name="emailEnabled">邮件通知</param>
        /// <param name="smsEnabled">短信通知</param>
        /// <param name="weChatEnabled">微信通知</param>
        public void UpdateNotificationPreferences(bool? emailEnabled, bool? smsEnabled, bool? weChatEnabled)
        {
            if (emailEnabled.HasValue)
                EmailNotificationEnabled = emailEnabled.Value;
            if (smsEnabled.HasValue)
                SmsNotificationEnabled = smsEnabled.Value;
            if (weChatEnabled.HasValue)
                WeChatNotificationEnabled = weChatEnabled.Value;
            UpdateTimestamp();
        }
    }
}

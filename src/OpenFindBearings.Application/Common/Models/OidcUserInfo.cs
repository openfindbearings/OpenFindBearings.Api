namespace OpenFindBearings.Application.Common.Models
{
    /// <summary>
    /// OIDC 标准用户信息
    /// 遵循 OpenID Connect 规范
    /// </summary>
    public class OidcUserInfo
    {
        /// <summary>
        /// 用户唯一标识 (sub)
        /// </summary>
        public string Sub { get; set; } = string.Empty;

        /// <summary>
        /// 全名或昵称 (name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 名 (given_name)
        /// </summary>
        public string? GivenName { get; set; }

        /// <summary>
        /// 姓 (family_name)
        /// </summary>
        public string? FamilyName { get; set; }

        /// <summary>
        /// 邮箱 (email)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 邮箱是否验证 (email_verified)
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// 手机号 (phone_number)
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 手机号是否验证 (phone_number_verified)
        /// </summary>
        public bool PhoneNumberVerified { get; set; }

        /// <summary>
        /// 首选用户名 (preferred_username)
        /// </summary>
        public string? PreferredUsername { get; set; }

        /// <summary>
        /// 头像URL (picture)
        /// </summary>
        public string? Picture { get; set; }

        /// <summary>
        /// 用户是否活跃
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName() => Name ?? PreferredUsername ?? Sub;
    }
}

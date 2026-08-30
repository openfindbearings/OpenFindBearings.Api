namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 限流用户类型常量
    /// 职责：定义限流分档标识，供用户上下文中间件与限流中间件共用，避免两处硬编码字符串不一致
    /// 说明：这些值与 SystemConfigs 中 RateLimit.* 配置项的档位一一对应
    /// </summary>
    public static class RateLimitUserType
    {
        /// <summary>
        /// 未登录游客，对应 RateLimit.Guest.RequestsPerMinute
        /// </summary>
        public const string Guest = "guest";

        /// <summary>
        /// 已登录普通用户，对应 RateLimit.User.RequestsPerMinute
        /// </summary>
        public const string User = "user";

        /// <summary>
        /// 付费用户，对应 RateLimit.Premium.RequestsPerMinute
        /// 说明：当前 User 聚合根虽保留 UserLevel 与 SubscriptionExpiry 字段，但尚无付费角色，
        ///       该档位暂不可达，属预留；待会员体系接入后由用户上下文中间件按等级推导
        /// </summary>
        public const string Vip = "vip";

        /// <summary>
        /// 商户员工，对应限流字典中的 merchant 档位
        /// </summary>
        public const string Merchant = "merchant";

        /// <summary>
        /// 管理员，对应限流字典中的 admin 档位
        /// </summary>
        public const string Admin = "admin";
    }
}

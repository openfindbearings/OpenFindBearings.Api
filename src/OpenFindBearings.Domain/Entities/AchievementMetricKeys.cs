namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 成就计量键常量（v2.13.2 收敛）：这些键必须与 AchievementDefinitions.MetricKey 种子值逐字一致，
    /// 此前以裸字符串散落在各事件处理器/端点/中间件里，任一处拼写漂移都会导致"该成就永不点亮"且难察觉。
    /// 集中定义后，调用方引用常量，种子迁移亦以此为准。
    /// </summary>
    public static class AchievementMetricKeys
    {
        /// <summary>纠错被采纳计数（个人轨）</summary>
        public const string CorrectionAdopted = "correction_adopted";
        /// <summary>累计签到次数（个人轨）</summary>
        public const string CheckinTotal = "checkin_total";
        /// <summary>连续签到天数仪表（个人轨）</summary>
        public const string CheckinStreak = "checkin_streak";
        /// <summary>累计登录次数（个人轨）</summary>
        public const string LoginTotal = "login_total";
        /// <summary>注册计数（个人轨）</summary>
        public const string RegisterTotal = "register_total";
        /// <summary>寻货应答被选定计数（个人轨）</summary>
        public const string SourcingSelected = "sourcing_selected";
        /// <summary>发布寻货计数（个人轨）</summary>
        public const string SourcingPublishTotal = "sourcing_publish_total";
        /// <summary>在售商品数仪表（商户轨）。
        /// 口径说明（v1.8.2）：本仪表保持"现货在售"口径，不随等级/寻货的"公域可见（含补货中）"口径统一——
        /// 成就阈值与历史进度回填已按现货口径固定，中途改口径会凭空白送"货架满满"勋章</summary>
        public const string ListingCount = "listing_count";
        /// <summary>商户应答被选定计数（商户轨）</summary>
        public const string MerchantSelected = "merchant_selected";
        /// <summary>商户通过认证仪表（商户轨）</summary>
        public const string MerchantVerified = "merchant_verified";
    }
}
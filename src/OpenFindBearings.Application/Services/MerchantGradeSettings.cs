namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商家等级规则配置键与默认值（v2.13.2 收敛）：等级重算服务（Infrastructure）与积分端点（Api）
    /// 读取同一份 SystemConfig 键与回退默认，消除 "5/10/500/50/150/300" 在多处重复硬编码的漂移风险。
    /// 实际值仍由 Admin 可调的 SystemConfig 决定，此处仅集中键名与缺省值。
    /// </summary>
    public static class MerchantGradeSettings
    {
        /// <summary>升 Lv3 口碑的在售件数下限配置键</summary>
        public const string PremiumOnSaleMinKey = "Business.MerchantPremiumOnSaleMin";
        /// <summary>升 Lv4 金牌的在售件数下限配置键</summary>
        public const string GoldOnSaleMinKey = "Business.MerchantGoldOnSaleMin";
        /// <summary>升 Lv4 金牌的金库累计下限配置键</summary>
        public const string GoldTreasuryMinKey = "Business.MerchantGoldTreasuryMin";
        /// <summary>Lv2 认证升档礼金额配置键</summary>
        public const string BonusLv2Key = "Business.MerchantGradeBonusLv2";
        /// <summary>Lv3 口碑升档礼金额配置键</summary>
        public const string BonusLv3Key = "Business.MerchantGradeBonusLv3";
        /// <summary>Lv4 金牌升档礼金额配置键</summary>
        public const string BonusLv4Key = "Business.MerchantGradeBonusLv4";
        /// <summary>降档保级缓冲天数配置键</summary>
        public const string GraceDaysKey = "Business.MerchantGradeGraceDays";

        /// <summary>升 Lv3 在售下限默认值</summary>
        public const int PremiumOnSaleMinDefault = 5;
        /// <summary>升 Lv4 在售下限默认值</summary>
        public const int GoldOnSaleMinDefault = 10;
        /// <summary>升 Lv4 金库累计下限默认值</summary>
        public const int GoldTreasuryMinDefault = 500;
        /// <summary>Lv2 升档礼默认值</summary>
        public const int BonusLv2Default = 50;
        /// <summary>Lv3 升档礼默认值</summary>
        public const int BonusLv3Default = 150;
        /// <summary>Lv4 升档礼默认值</summary>
        public const int BonusLv4Default = 300;
        /// <summary>保级缓冲天数默认值</summary>
        public const int GraceDaysDefault = 15;
    }
}
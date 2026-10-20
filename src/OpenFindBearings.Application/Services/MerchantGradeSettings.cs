namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商家牌级规则配置键与默认值（v2.13.2 收敛建表，v2.19.0 改为有效期制）：
    /// 等级重算服务（Infrastructure）与积分端点（Api）读取同一份 SystemConfig 键与回退默认，
    /// 消除阈值在多处重复硬编码的漂移风险；实际值仍由 Admin 可调的 SystemConfig 决定。
    /// 改动说明（v2.19.0）：定级改为「认证门槛 + 实时在售件数 + 本有效期成交次数」；
    ///   WindowDays 由"滚动统计窗口"改为"牌级有效期天数"（键名不变，避免存量配置失效）。
    /// </summary>
    public static class MerchantGradeSettings
    {
        /// <summary>升牌共同前置：公域可见在售件数下限配置键（银牌/金牌都需满足）</summary>
        public const string GradeOnSaleMinKey = "Business.MerchantGradeOnSaleMin";
        /// <summary>银牌所需的有效期成交次数下限配置键</summary>
        public const string SilverDealMinKey = "Business.MerchantSilverDealMin";
        /// <summary>金牌所需的有效期成交次数下限配置键</summary>
        public const string GoldDealMinKey = "Business.MerchantGoldDealMin";
        /// <summary>牌级有效期天数配置键（升档/续期各发一个周期）</summary>
        public const string WindowDaysKey = "Business.MerchantGradeWindowDays";
        /// <summary>升铜牌（认证）档礼金额配置键</summary>
        public const string BonusLv2Key = "Business.MerchantGradeBonusLv2";
        /// <summary>升银牌档礼金额配置键</summary>
        public const string BonusLv3Key = "Business.MerchantGradeBonusLv3";
        /// <summary>升金牌档礼金额配置键</summary>
        public const string BonusLv4Key = "Business.MerchantGradeBonusLv4";
        /// <summary>在售不足保级缓冲天数配置键</summary>
        public const string GraceDaysKey = "Business.MerchantGradeGraceDays";

        /// <summary>升牌共同前置在售件数默认值</summary>
        public const int GradeOnSaleMinDefault = 5;
        /// <summary>银牌成交次数默认值</summary>
        public const int SilverDealMinDefault = 1;
        /// <summary>金牌成交次数默认值</summary>
        public const int GoldDealMinDefault = 3;
        /// <summary>牌级有效期默认天数</summary>
        public const int WindowDaysDefault = 180;
        /// <summary>铜牌升档礼默认值</summary>
        public const int BonusLv2Default = 50;
        /// <summary>银牌升档礼默认值</summary>
        public const int BonusLv3Default = 150;
        /// <summary>金牌升档礼默认值</summary>
        public const int BonusLv4Default = 300;
        /// <summary>在售不足保级天数默认值</summary>
        public const int GraceDaysDefault = 15;
    }
}

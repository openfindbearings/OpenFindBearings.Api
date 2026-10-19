namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商家卡级规则配置键与默认值（v2.13.2 收敛建表，v2.18.0 卡级重构改口径）：
    /// 等级重算服务（Infrastructure）与积分端点（Api）读取同一份 SystemConfig 键与回退默认，
    /// 消除阈值在多处重复硬编码的漂移风险；实际值仍由 Admin 可调的 SystemConfig 决定。
    /// 改动说明（v2.18.0）：定级口径改为「认证门槛 + 在售件数 + 近窗口期成功撮合次数」，
    ///   删除金库相关键（MerchantGoldTreasuryMin）与分档在售键（Premium/GoldOnSaleMin），
    ///   合并为统一的在售前置键 GradeOnSaleMin，并新增窗口天数与两档撮合次数键。
    /// </summary>
    public static class MerchantGradeSettings
    {
        /// <summary>升卡共同前置：公域可见在售件数下限配置键（银卡/金卡都需满足）</summary>
        public const string GradeOnSaleMinKey = "Business.MerchantGradeOnSaleMin";
        /// <summary>银卡所需的窗口期成功撮合次数下限配置键</summary>
        public const string SilverDealMinKey = "Business.MerchantSilverDealMin";
        /// <summary>金卡所需的窗口期成功撮合次数下限配置键</summary>
        public const string GoldDealMinKey = "Business.MerchantGoldDealMin";
        /// <summary>成功撮合次数统计的滚动窗口天数配置键</summary>
        public const string WindowDaysKey = "Business.MerchantGradeWindowDays";
        /// <summary>升铜卡（认证）档礼金额配置键</summary>
        public const string BonusLv2Key = "Business.MerchantGradeBonusLv2";
        /// <summary>升银卡档礼金额配置键</summary>
        public const string BonusLv3Key = "Business.MerchantGradeBonusLv3";
        /// <summary>升金卡档礼金额配置键</summary>
        public const string BonusLv4Key = "Business.MerchantGradeBonusLv4";
        /// <summary>降档保级缓冲天数配置键</summary>
        public const string GraceDaysKey = "Business.MerchantGradeGraceDays";

        /// <summary>升卡共同前置在售件数默认值</summary>
        public const int GradeOnSaleMinDefault = 5;
        /// <summary>银卡成功撮合次数默认值</summary>
        public const int SilverDealMinDefault = 1;
        /// <summary>金卡成功撮合次数默认值</summary>
        public const int GoldDealMinDefault = 3;
        /// <summary>撮合统计滚动窗口天数默认值</summary>
        public const int WindowDaysDefault = 180;
        /// <summary>铜卡升档礼默认值</summary>
        public const int BonusLv2Default = 50;
        /// <summary>银卡升档礼默认值</summary>
        public const int BonusLv3Default = 150;
        /// <summary>金卡升档礼默认值</summary>
        public const int BonusLv4Default = 300;
        /// <summary>保级缓冲天数默认值</summary>
        public const int GraceDaysDefault = 15;
    }
}

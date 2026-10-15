namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 轴承币与商家金规则数据源 DTO（v2.17.0 规则数据源）。
    /// 聚合积分规则表 / 段位表 / 集体任务定义 / SystemConfig / MerchantBuffs 常量的全部对外数值，
    /// 供 Taro 规则页渲染；每字段默认值与发放侧代码默认一致，缺失即用默认，保证页面不因数据缺失变形。
    /// 内部分值与接口口径注释：轴承币=points，商家金=gold，仅展示层命名区分，代码保持原口径。
    /// </summary>
    public class PointRulesDto
    {
        // ============ 轴承币（points） ============

        /// <summary>签到连签阶梯（连续天数 1/2/3/... 取值，超出取末档；默认 [2,3,4,5,5]）</summary>
        public int[] CheckinLadder { get; set; } = new[] { 2, 3, 4, 5, 5 };

        /// <summary>签到暴击双倍概率（百分比，默认 10）</summary>
        public int CheckinDoubleChance { get; set; } = 10;

        /// <summary>签到传说 ×5 暴击概率（百分比，默认 2）</summary>
        public int CheckinLegendChance { get; set; } = 2;

        /// <summary>每日登录分值（默认 1）</summary>
        public int LoginAmount { get; set; } = 1;

        /// <summary>纠错被采纳分值（默认 20）</summary>
        public int CorrectionAmount { get; set; } = 20;

        /// <summary>纠错被采纳每日上限（默认 100）</summary>
        public int CorrectionDailyLimit { get; set; } = 100;

        /// <summary>每日三件套额外奖励分值（默认 30）</summary>
        public int ComboAmount { get; set; } = 30;

        /// <summary>小游戏每局分值（默认 5）</summary>
        public int MinigameAmount { get; set; } = 5;

        /// <summary>小游戏每日上限分值（默认 10）</summary>
        public int MinigameDailyLimit { get; set; } = 10;

        /// <summary>完善资料一次性里程碑分值（默认 50）</summary>
        public int MilestoneProfile { get; set; } = 50;

        /// <summary>首次上架一次性里程碑分值（默认 20）</summary>
        public int MilestoneFirstProduct { get; set; } = 20;

        /// <summary>入驻审批通过一次性里程碑分值（默认 100）</summary>
        public int MilestoneApproved { get; set; } = 100;

        /// <summary>寻货超额每次扣币分值（默认 20）</summary>
        public int SourcingBonusAmount { get; set; } = 20;

        /// <summary>寻货发布每日硬上限条数（默认 10）</summary>
        public int SourcingPublishDailyLimit { get; set; } = 10;

        /// <summary>寻货应答每日硬上限条数（默认 50）</summary>
        public int SourcingRespondDailyLimit { get; set; } = 50;

        /// <summary>寻货每日免费发布条数（默认 3）</summary>
        public int FreePublishPerDay { get; set; } = 3;

        /// <summary>寻货每日免费应答条数（默认 20）</summary>
        public int FreeRespondPerDay { get; set; } = 20;

        /// <summary>段位升档礼区间下限（默认 10）</summary>
        public int LevelBonusMin { get; set; } = 10;

        /// <summary>段位升档礼区间上限（默认 120）</summary>
        public int LevelBonusMax { get; set; } = 120;

        /// <summary>业务切日时区偏移小时数（默认 8，即东八区/北京时间）</summary>
        public int TzOffsetHours { get; set; } = 8;

        // ============ buff（MerchantBuffs 代码常量） ============

        /// <summary>签到 buff 最大加数（默认 3）</summary>
        public int BuffCheckinMax { get; set; } = 3;

        /// <summary>每日登录 buff 加数（默认 1）</summary>
        public int BuffLogin { get; set; } = 1;

        /// <summary>纠错采纳 buff 最大乘数（默认 1.25）</summary>
        public double BuffCorrectionMax { get; set; } = 1.25;

        // ============ 商家金（gold，SystemConfig 驱动） ============

        /// <summary>成员上供比例（百分比，默认 10）</summary>
        public int TricklePercent { get; set; } = 10;

        /// <summary>店铺每日上供入账上限（默认 50）</summary>
        public int TrickleDailyCap { get; set; } = 50;

        /// <summary>店铺每月上供入账上限（默认 1000）</summary>
        public int TrickleMonthlyCap { get; set; } = 1000;

        /// <summary>挂礼结算每月上限（默认 2000）</summary>
        public int GiftSettleMonthlyCap { get; set; } = 2000;

        /// <summary>商家升到 Lv2 的升档礼金额（默认 50）</summary>
        public int MerchantGradeBonusLv2 { get; set; } = 50;

        /// <summary>商家升到 Lv3 的升档礼金额（默认 150）</summary>
        public int MerchantGradeBonusLv3 { get; set; } = 150;

        /// <summary>商家升到 Lv4 的升档礼金额（默认 300）</summary>
        public int MerchantGradeBonusLv4 { get; set; } = 300;

        /// <summary>商家掉级缓冲天数（默认 15）</summary>
        public int MerchantGradeGraceDays { get; set; } = 15;

        /// <summary>个人代付汇率（1 商家金折算 N 轴承币，默认 2）</summary>
        public int MerchantGoldPayRate { get; set; } = 2;

        /// <summary>集体任务周奖励金库金额（默认 100）</summary>
        public int MerchantTaskWeeklyTreasury { get; set; } = 100;

        /// <summary>集体任务成员奖区间下限（默认 20）</summary>
        public int MerchantTaskMemberMin { get; set; } = 20;

        /// <summary>集体任务成员奖区间上限（默认 50）</summary>
        public int MerchantTaskMemberMax { get; set; } = 50;
    }
}
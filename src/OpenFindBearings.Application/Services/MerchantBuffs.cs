using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商家 buff 目录（v2.5.0 商家经济）：等级 → 被动加成的静态规则表（承设计 6977 定案）。
    /// 全部为"赚取加数/乘数、额度加数、消费折扣"四类的纯查表——无金库提分到个人路径（合规红线）；
    /// 加成结果仍受 PointGrantRule 日上限约束（先加成后截顶）
    /// </summary>
    public static class MerchantBuffs
    {
        // 改动说明（v2.17.0 规则数据源）：把 buff 上限抽取为具名常量，供规则页下发复用，
        // 避免"页面文案 +3 / ×1.25"与代码实现双份维护而漂移
        /// <summary>签到 buff 最大加数（金牌 +3，规则页"最高 +N"展示用）</summary>
        public const int CheckinBonusMax = 3;

        /// <summary>每日登录 buff 加数（认证及以上 +1）</summary>
        public const int LoginBonusMax = 1;

        /// <summary>纠错采纳 buff 最大乘数（金牌 ×1.25）</summary>
        public const double CorrectionMultiplierMax = 1.25;

        /// <summary>等级数值 → 等级序数（Ordinary=1 普通 / Bronze=2 铜牌 / Silver=3 银牌 / Gold=4 金牌）。
        /// 改动说明（v2.13.2 序数统一）：委托 Domain.MerchantGradeExtensions.Rank，消除枚举数值映射的二次维护
        /// 改动说明（v2.19.0 牌级制）：枚举名与展示名同步为"普通/铜牌/银牌/金牌"</summary>
        public static int Rank(int grade) => ((MerchantGrade)grade).Rank();

        /// <summary>每日签到加数（仅持牌商户：铜牌 +1 / 银牌 +2 / 金牌 +3；普通档无加成）</summary>
        public static int CheckinBonus(int grade) => Rank(grade) switch { >= 4 => CheckinBonusMax, 3 => 2, >= 2 => 1, _ => 0 };

        /// <summary>每日登录加数（Lv2 起 +1）</summary>
        public static int LoginBonus(int grade) => Rank(grade) >= 2 ? LoginBonusMax : 0;

        /// <summary>纠错被采纳乘数（Lv2 ×1.1 / Lv3 ×1.2 / Lv4 ×1.25），返回加成后的整数值</summary>
        public static int ApplyCorrectionBonus(int baseAmount, int grade) => Rank(grade) switch
        {
            >= 4 => (int)Math.Ceiling(baseAmount * CorrectionMultiplierMax),
            3 => (int)Math.Ceiling(baseAmount * 1.20),
            2 => (int)Math.Ceiling(baseAmount * 1.10),
            _ => baseAmount
        };

        /// <summary>寻货应答免费额度加数（仅持牌商户：铜牌 +1 / 银牌 +3 / 金牌 +5；普通档无加成）</summary>
        public static int RespondQuotaBonus(int grade) => Rank(grade) switch { >= 4 => 5, 3 => 3, >= 2 => 1, _ => 0 };

        /// <summary>寻货发布免费额度加数（Lv2 起 +1）</summary>
        public static int PublishQuotaBonus(int grade) => Rank(grade) >= 2 ? 1 : 0;

        /// <summary>置顶卡折扣（Lv3 九五不折 → 定案九折 / Lv4 八折），返回折扣后整数价</summary>
        public static int ApplyPinDiscount(int price, int grade) => Rank(grade) switch
        {
            >= 4 => (int)Math.Floor(price * 0.80),
            3 => (int)Math.Floor(price * 0.90),
            _ => price
        };

        /// <summary>
        /// buff 文案清单（任务中心商家福利卡、牌级页权益清单展示用，按等级序数）。
        /// 改动说明（v2.19.0）：每条都写明"加什么"——个人轴承币、每日免费额度或置顶费折扣，
        /// 避免"每日签到 +1"被误解为商家金或签到次数
        /// </summary>
        public static List<string> BuffLabels(int grade)
        {
            var r = Rank(grade);
            // 改动说明（v2.19.0 审计修复）：普通档（未认证或在售不足）无任何商家加成，
            // 与规则页"普通无等级权益"及"认证后在售达标解锁"口径一致
            if (r <= 1) return new List<string>();
            var labels = new List<string> { $"每日签到：个人轴承币 +{CheckinBonus(grade)}" };
            if (r >= 2) labels.Add($"每日登录：个人轴承币 +{LoginBonusMax}");
            if (r >= 2) labels.Add($"纠错被采纳：个人轴承币 ×{CorrectionMultiplier(r)}");
            labels.Add($"寻货应答：每日免费额度 +{RespondQuotaBonus(grade)} 次");
            if (r >= 2) labels.Add("寻货发布：每日免费额度 +1 条");
            if (r >= 3) labels.Add($"商品置顶费：{(r >= 4 ? "8" : "9")} 折");
            return labels;
        }

        /// <summary>纠错加成倍数的展示文本（1.1 / 1.2 / 1.25），与 ApplyCorrectionBonus 同口径</summary>
        private static string CorrectionMultiplier(int rank) => rank switch
        {
            >= 4 => "1.25",
            3 => "1.2",
            _ => "1.1"
        };
    }
}

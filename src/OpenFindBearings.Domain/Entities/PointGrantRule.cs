using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 积分发放规则实体：各赚分动作的分值/每日上限/连续阶梯/开关，
    /// Admin"积分任务管理"页驱动，GrantAsync 每次实时读表——调分不发版
    /// </summary>
    public class PointGrantRule : BaseEntity
    {
        /// <summary>动作类型（与 PointTransaction.GrantType 对应，唯一）</summary>
        public string GrantType { get; private set; } = string.Empty;

        /// <summary>动作中文名（Admin 列表与前端明细展示）</summary>
        public string DisplayName { get; private set; } = string.Empty;

        /// <summary>基础分值（无阶梯时即每笔分值；有阶梯时为起步值）</summary>
        public int Amount { get; private set; }

        /// <summary>每日上限（当日该动作累计发放封顶，0=不限）——防刷核心</summary>
        public int DailyLimit { get; private set; }

        /// <summary>连续阶梯 JSON 数组（如 [2,3,4,5,5]，按连续天数取档，超出取末档；null=固定分值）</summary>
        public string? LadderJson { get; private set; }

        /// <summary>是否启用（运营可暂停某赚分口而不删规则）</summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>规则说明</summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 双倍暴击概率（0-100，默认 0=不暴击；v2.8.0 G1）。服务端 GrantAsync/CheckinAsync
        /// 发放时 RNG 判定：命中则分值 ×2；仍受 DailyLimit 截断（金额不足时余量封顶为剩余额度）
        /// </summary>
        public int DoubleChance { get; private set; }

        /// <summary>
        /// 传说暴击概率（0-100，默认 0=不暴击；v2.8.0 G1）。判定优先级高于双倍，
        /// 命中则分值 ×5（"传说掉落"）；仍受 DailyLimit 截断
        /// </summary>
        public int LegendChance { get; private set; }

        /// <summary>EF 构造函数</summary>
        protected PointGrantRule() { }

        /// <summary>
        /// 新建规则
        /// </summary>
        /// <param name="grantType">动作类型</param>
        /// <param name="displayName">中文名</param>
        /// <param name="amount">基础分值</param>
        /// <param name="dailyLimit">每日上限（0=不限）</param>
        /// <param name="ladderJson">连续阶梯 JSON（可空）</param>
        /// <param name="description">说明（可空）</param>
        /// <param name="doubleChance">双倍暴击概率（0-100，默认 0）</param>
        /// <param name="legendChance">传说暴击概率（0-100，默认 0）</param>
        public PointGrantRule(string grantType, string displayName, int amount,
            int dailyLimit = 0, string? ladderJson = null, string? description = null,
            int doubleChance = 0, int legendChance = 0)
        {
            GrantType = grantType;
            DisplayName = displayName;
            Amount = amount;
            DailyLimit = dailyLimit;
            LadderJson = ladderJson;
            Description = description;
            IsEnabled = true;
            DoubleChance = doubleChance;
            LegendChance = legendChance;
        }

        /// <summary>
        /// 调整分值（Admin 配置入口）
        /// </summary>
        /// <param name="amount">新基础分值</param>
        public void ChangeAmount(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("分值不能为负", nameof(amount));
            Amount = amount;
            UpdateTimestamp();
        }

        /// <summary>
        /// 调整每日上限
        /// </summary>
        /// <param name="dailyLimit">新上限（0=不限）</param>
        public void ChangeDailyLimit(int dailyLimit)
        {
            if (dailyLimit < 0)
                throw new ArgumentException("每日上限不能为负", nameof(dailyLimit));
            DailyLimit = dailyLimit;
            UpdateTimestamp();
        }

        /// <summary>
        /// 调整连续阶梯
        /// </summary>
        /// <param name="ladderJson">阶梯 JSON 数组（null=取消阶梯）</param>
        public void ChangeLadder(string? ladderJson)
        {
            LadderJson = ladderJson;
            UpdateTimestamp();
        }

        /// <summary>
        /// 启用/停用规则（暂停赚分口不发版）
        /// </summary>
        /// <param name="enabled">目标状态</param>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            UpdateTimestamp();
        }

        /// <summary>
        /// 调整暴击概率（v2.8.0 G1）：双倍与传说概率各 0-100，任一起 0 即该档不暴击
        /// </summary>
        /// <param name="doubleChance">双倍暴击概率（0-100）</param>
        /// <param name="legendChance">传说暴击概率（0-100）</param>
        public void ChangeCritChances(int doubleChance, int legendChance)
        {
            if (doubleChance < 0 || doubleChance > 100)
                throw new ArgumentException("双倍暴击概率须在 0-100 之间", nameof(doubleChance));
            if (legendChance < 0 || legendChance > 100)
                throw new ArgumentException("传说暴击概率须在 0-100 之间", nameof(legendChance));
            if (doubleChance + legendChance > 100)
                throw new ArgumentException("双倍与传说概率之和不能超过 100", nameof(doubleChance));
            DoubleChance = doubleChance;
            LegendChance = legendChance;
            UpdateTimestamp();
        }
    }
}

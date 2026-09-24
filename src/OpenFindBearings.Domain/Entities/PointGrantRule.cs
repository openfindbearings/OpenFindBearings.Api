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
        public PointGrantRule(string grantType, string displayName, int amount,
            int dailyLimit = 0, string? ladderJson = null, string? description = null)
        {
            GrantType = grantType;
            DisplayName = displayName;
            Amount = amount;
            DailyLimit = dailyLimit;
            LadderJson = ladderJson;
            Description = description;
            IsEnabled = true;
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
    }
}

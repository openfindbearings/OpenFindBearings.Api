using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 工会集体任务定义（v2.6.0 M3 集体任务）：全公会成员共同推进的周期性目标（帮派 raid 类比）。
    /// 达标即结算（不等周期结束），同周期防重复由 GuildTaskCompletion 台账唯一键保证；
    /// 数值与开关 Admin 可配实时生效（与积分规则表同治理模式）
    /// </summary>
    public class GuildTaskDefinition : BaseEntity
    {
        /// <summary>周期：每自然周（业务日界口径）</summary>
        public const int PeriodWeekly = 1;
        /// <summary>周期：每自然月（业务日界口径）</summary>
        public const int PeriodMonthly = 2;

        /// <summary>奖励对象：全体在职成员各得一笔积分</summary>
        public const int RewardMembers = 1;
        /// <summary>奖励对象：商家金库入账</summary>
        public const int RewardTreasury = 2;

        /// <summary>任务键（唯一，奖励 bizId 前缀与指标路由依据，如 guild_corrections_week）</summary>
        public string TaskKey { get; private set; } = string.Empty;

        /// <summary>任务名（成员可见文案）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>任务描述（目标与奖励说明）</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>指标键：corrections=本周期成员纠错被采纳数 / treasury=本期金库入账总额 / products=本期新上架商品数</summary>
        public string MetricKey { get; private set; } = string.Empty;

        /// <summary>达标目标值（指标聚合 >= 此数即完成）</summary>
        public int TargetValue { get; private set; }

        /// <summary>周期（1 周 / 2 月）</summary>
        public int Period { get; private set; } = PeriodWeekly;

        /// <summary>奖励对象（1 全体成员 / 2 金库）</summary>
        public int RewardType { get; private set; } = RewardMembers;

        /// <summary>奖励分值（成员=每人所得；金库=入账额）</summary>
        public int RewardAmount { get; private set; }

        /// <summary>是否启用（Admin 开关，实时生效）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>排序权重（小者靠前）</summary>
        public int SortOrder { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected GuildTaskDefinition() { }

        /// <summary>创建集体任务（Admin 新建/迁移种子共用）</summary>
        public static GuildTaskDefinition Create(string taskKey, string name, string description,
            string metricKey, int targetValue, int period, int rewardType, int rewardAmount, int sortOrder)
        {
            return new GuildTaskDefinition
            {
                TaskKey = taskKey,
                Name = name,
                Description = description,
                MetricKey = metricKey,
                TargetValue = targetValue,
                Period = period,
                RewardType = rewardType,
                RewardAmount = rewardAmount,
                Enabled = true,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>Admin 编辑（TaskKey 不可变——台账与奖励 bizId 锚定它）</summary>
        public void Update(string name, string description, string metricKey, int targetValue,
            int period, int rewardType, int rewardAmount, bool enabled, int sortOrder)
        {
            Name = name;
            Description = description;
            MetricKey = metricKey;
            TargetValue = targetValue;
            Period = period;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
            Enabled = enabled;
            SortOrder = sortOrder;
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// 工会集体任务完成台账（v2.6.0）：一个任务×一个商户×一个周期只结算一次。
    /// (TaskKey, MerchantId, PeriodKey) 唯一索引=防重铁闸；PeriodKey 为业务日界周期串
    /// （周=周一 yyyyMMdd，月=yyyyMM，BusinessClock 口径与幂等键一致）
    /// </summary>
    public class GuildTaskCompletion : BaseEntity
    {
        /// <summary>任务键</summary>
        public string TaskKey { get; private set; } = string.Empty;

        /// <summary>商户 ID</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>周期标识（周=周一 DateKey / 月=yyyyMM）</summary>
        public string PeriodKey { get; private set; } = string.Empty;

        /// <summary>结算时的指标实际值（台账留痕，便于运营复盘）</summary>
        public int MetricValue { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected GuildTaskCompletion() { }

        /// <summary>记一笔完成（达标结算时写入；唯一索引冲突即该周期已结算）</summary>
        public GuildTaskCompletion(string taskKey, Guid merchantId, string periodKey, int metricValue)
        {
            TaskKey = taskKey;
            MerchantId = merchantId;
            PeriodKey = periodKey;
            MetricValue = metricValue;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }
    }
}

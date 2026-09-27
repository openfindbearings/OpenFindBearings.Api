using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 成就归属范围（v2.1.0 成就子系统）：Personal=个人轨（OwnerId=UserId），
    /// Merchant=商户/商家轨（OwnerId=MerchantId）。定义与解锁共用此枚举，一表双轨
    /// </summary>
    public enum AchievementScope
    {
        /// <summary>个人成就（挂个人资料页徽章排）</summary>
        Personal = 1,

        /// <summary>商户/商家成就（挂商户详情+卡片徽章排，B2B 信任信号）</summary>
        Merchant = 2
    }

    /// <summary>
    /// 成就定义（v2.1.0 成就子系统，WoW 成就墙模型）：一行=一个可点亮成就。
    /// MetricKey 是它追踪的计数器/仪表键（事件驱动累加或设值），ProgressTarget 是点亮阈值。
    /// MetaPoints=成就点（只加不花的炫耀 meta 分，读时按已解锁求和，不入库累加）；
    /// RewardPoints=解锁时一次性发放的可花积分甜头（小额，走 PointsService 幂等）
    /// </summary>
    public class AchievementDefinition : BaseEntity
    {
        /// <summary>成就唯一键（如 correction_first / checkin_streak_30 / merchant_verified）</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>成就名称（成就墙展示）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>成就描述 / 未解锁时的"如何获得"提示</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>图标名（前端 Icon 组件键）</summary>
        public string Icon { get; private set; } = string.Empty;

        /// <summary>归属范围（个人/商户）</summary>
        public AchievementScope Scope { get; private set; }

        /// <summary>分类（数据共创/寻货/商户/忠诚/彩蛋，成就墙分组）</summary>
        public string Category { get; private set; } = string.Empty;

        /// <summary>追踪的计数/仪表键（事件驱动 Increment/SetGauge）</summary>
        public string MetricKey { get; private set; } = string.Empty;

        /// <summary>点亮阈值（进度 >= 此值解锁）</summary>
        public int ProgressTarget { get; private set; }

        /// <summary>成就点（meta 炫耀分，只加不花）</summary>
        public int MetaPoints { get; private set; }

        /// <summary>解锁一次性可花积分甜头（0=不发）</summary>
        public int RewardPoints { get; private set; }

        /// <summary>解锁授予的称号（可空；挂昵称旁）</summary>
        public string? TitleReward { get; private set; }

        /// <summary>稀有成就（墙上加稀有标）</summary>
        public bool Rare { get; private set; }

        /// <summary>隐藏成就（未解锁不在墙上显示，解锁后才现身）</summary>
        public bool Hidden { get; private set; }

        /// <summary>是否启用（Admin 可停用以隐藏）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>EF 专用无参构造</summary>
        protected AchievementDefinition() { }

        /// <summary>
        /// 创建成就定义
        /// </summary>
        public AchievementDefinition(string key, string name, string description, string icon,
            AchievementScope scope, string category, string metricKey, int progressTarget,
            int metaPoints, int rewardPoints = 0, string? titleReward = null,
            bool rare = false, bool hidden = false)
        {
            Key = key;
            Name = name;
            Description = description;
            Icon = icon;
            Scope = scope;
            Category = category;
            MetricKey = metricKey;
            ProgressTarget = progressTarget;
            MetaPoints = metaPoints;
            RewardPoints = rewardPoints;
            TitleReward = titleReward;
            Rare = rare;
            Hidden = hidden;
        }

        /// <summary>Admin 编辑（分值/阈值/启停/文案）</summary>
        public void Update(string name, string description, int progressTarget, int metaPoints,
            int rewardPoints, string? titleReward, bool enabled)
        {
            Name = name;
            Description = description;
            ProgressTarget = progressTarget;
            MetaPoints = metaPoints;
            RewardPoints = rewardPoints;
            TitleReward = titleReward;
            Enabled = enabled;
        }
    }
}

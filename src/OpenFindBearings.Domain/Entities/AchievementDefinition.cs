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
    /// MetaPoints=成就点（只加不花的炫耀 meta 分，读时按已解锁求和，不入库累加）。
    /// 改动说明（v2.12.0 等级玩法）：成就纯荣誉化——删除 RewardPoints 发币字段，
    /// 解锁只点亮徽章与授予荣誉分/称号，可花货币激励统一收拢到段位升档礼一条线（防双轨农场）
    /// </summary>
    public class AchievementDefinition : BaseEntity
    {
        /// <summary>成就唯一键（如 correction_first / checkin_streak_30 / merchant_verified）</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>成就名称（成就墙展示）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>成就描述 / 未解锁时的"如何获得"提示</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>图标名（前端 Icon 组件键，无图时的占位图标）</summary>
        public string Icon { get; private set; } = string.Empty;

        /// <summary>
        /// 勋章图片相对媒体键（可空；v2.6.0 勋章图片管线上线后，后台上传的可替换图片来源）。
        /// 相对键入库，展示层由 Taro usableImage 统一解析为绝对地址——键变则 URL 变，
        /// 替换无需发版且无缓存残留（与商家 Logo / 用户头像同一管线）
        /// </summary>
        public string? ImageKey { get; private set; }

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

        /// <summary>解锁授予的称号（可空；挂昵称旁）</summary>
        public string? TitleReward { get; private set; }

        /// <summary>稀有成就（墙上加稀有标）</summary>
        public bool Rare { get; private set; }

        /// <summary>隐藏成就（未解锁不在墙上显示，解锁后才现身）</summary>
        public bool Hidden { get; private set; }

        /// <summary>
        /// 限量徽章（v2.8.0 G11）：true=有注册序号窗口限制，窗口关闭后绝版不再返场。
        /// 执行纪律：限量徽章永不补发、永不回填，错过了就是错过（Feats of Strength 模型）
        /// </summary>
        public bool IsLimited { get; private set; }

        /// <summary>
        /// 限量窗口序号（v2.8.0 G11，IsLimited=true 时生效）：注册序号 ≤ 此值的用户可解锁，
        /// 之后注册的用户窗口关闭永远拿不到（如"创站元老"=前 100 注册）
        /// </summary>
        public int? LimitedOrdinal { get; private set; }

        /// <summary>是否启用（Admin 可停用以隐藏）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>EF 专用无参构造</summary>
        protected AchievementDefinition() { }

        /// <summary>
        /// 创建成就定义
        /// </summary>
        public AchievementDefinition(string key, string name, string description, string icon,
            AchievementScope scope, string category, string metricKey, int progressTarget,
            int metaPoints, string? titleReward = null,
            bool rare = false, bool hidden = false, bool isLimited = false, int? limitedOrdinal = null)
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
            TitleReward = titleReward;
            Rare = rare;
            Hidden = hidden;
            // 改动说明（v2.8.0 G11）：限量字段默认非限量；限量必须带窗口序号（否则无意义）
            IsLimited = isLimited;
            LimitedOrdinal = isLimited ? (limitedOrdinal ?? 0) : null;
        }

        /// <summary>Admin 编辑（阈值/启停/文案/勋章图键；v2.12.0 起不含发币字段——成就纯荣誉化）</summary>
        public void Update(string name, string description, int progressTarget, int metaPoints,
            string? titleReward, bool enabled, string? imageKey = null,
            bool isLimited = false, int? limitedOrdinal = null)
        {
            Name = name;
            Description = description;
            ProgressTarget = progressTarget;
            MetaPoints = metaPoints;
            TitleReward = titleReward;
            Enabled = enabled;
            // 改动说明（v2.8.0 G11）：限量字段随编辑写回（限量必须带窗口序号）
            IsLimited = isLimited;
            LimitedOrdinal = isLimited ? (limitedOrdinal ?? 0) : null;
            // 改动说明（v2.6.0）：勋章图键随编辑一并写回；单独上传端点走专用方法设置
            if (imageKey is not null)
                ImageKey = imageKey;
        }

        /// <summary>
        /// 替换勋章图片键（v2.6.0 上传端点专用：上传成功后更新键并刷新更新时间戳，
        /// 旧键由上传端点先删除，保证可替换且不留孤儿对象）
        /// </summary>
        public void SetImageKey(string imageKey)
        {
            ImageKey = imageKey;
            UpdateTimestamp();
        }
    }
}

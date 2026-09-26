using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商城商品（v2.3.0 商城虚拟权益）：平台统一定价的虚拟权益目录。
    /// 一期仅置顶卡实装履约；闪购=限时改价窗口（零成本运营手段，Admin 可配）；
    /// 价格由平台统一设定而非商户自定，杜绝定向积分转移
    /// </summary>
    public class MallItem : BaseEntity
    {
        /// <summary>商品键（唯一，履约策略与幂等前缀依据，如 pin_24h）</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>商品名（用户可见）</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>描述/权益说明（用户可见，兼"如何生效"提示）</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>图标名（前端 Icon 组件键）</summary>
        public string Icon { get; private set; } = "gift";

        /// <summary>类别（决定履约策略）</summary>
        public MallItemCategory Category { get; private set; } = MallItemCategory.PinCard;

        /// <summary>常规积分单价（平台统一定价，Admin 可改，实时生效）</summary>
        public int PointPrice { get; private set; }

        /// <summary>闪购价（可空=无闪购；仅闪购窗口内生效，窗口外自动回落常规价）</summary>
        public int? FlashPrice { get; private set; }

        /// <summary>闪购开始（UTC，可空）</summary>
        public DateTime? FlashStart { get; private set; }

        /// <summary>闪购结束（UTC，可空）</summary>
        public DateTime? FlashEnd { get; private set; }

        /// <summary>权益时长（小时）：置顶卡=置顶时长；其他类别可空</summary>
        public int? DurationHours { get; private set; }

        /// <summary>库存（-1=不限量；限量制造稀缺感，兑完即止）</summary>
        public int Stock { get; private set; } = -1;

        /// <summary>已售数量（限量商品扣减依据+从众信号展示）</summary>
        public int SoldCount { get; private set; }

        /// <summary>是否上架（Admin 开关，实时生效不发版）</summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>排序权重（小者靠前）</summary>
        public int SortOrder { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected MallItem() { }

        /// <summary>
        /// 创建商品（迁移种子/Admin 新建共用）
        /// </summary>
        public static MallItem Create(string key, string name, string description, string icon,
            MallItemCategory category, int pointPrice, int? durationHours, int stock, int sortOrder)
        {
            return new MallItem
            {
                Key = key,
                Name = name,
                Description = description,
                Icon = icon,
                Category = category,
                PointPrice = pointPrice,
                DurationHours = durationHours,
                Stock = stock,
                SortOrder = sortOrder,
                Enabled = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 当前生效价（闪购窗口内取闪购价，否则常规价）。
        /// 窗口判定用调用方传入的 now（UTC），便于测试与统一口径
        /// </summary>
        public int EffectivePrice(DateTime nowUtc)
        {
            if (FlashPrice.HasValue && FlashStart.HasValue && FlashEnd.HasValue
                && nowUtc >= FlashStart.Value && nowUtc <= FlashEnd.Value)
                return FlashPrice.Value;
            return PointPrice;
        }

        /// <summary>是否处于闪购窗口（前端角标/倒计时依据）</summary>
        public bool IsFlashing(DateTime nowUtc) =>
            FlashPrice.HasValue && FlashStart.HasValue && FlashEnd.HasValue
            && nowUtc >= FlashStart.Value && nowUtc <= FlashEnd.Value;

        /// <summary>是否还有库存（-1 不限；否则 SoldCount &lt; Stock）</summary>
        public bool HasStock() => Stock < 0 || SoldCount < Stock;

        /// <summary>
        /// 扣库存（兑换履约成功后调用）。限量商品超卖守卫由调用方在事务内重读保证
        /// </summary>
        public void ConsumeStock()
        {
            SoldCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Admin 编辑（价格/闪购窗口/库存/上下架/文案/排序）。
        /// Key 与 Category 不可变——履约策略与历史订单快照都锚定它们
        /// </summary>
        public void Update(string name, string description, string icon, int pointPrice,
            int? flashPrice, DateTime? flashStart, DateTime? flashEnd,
            int? durationHours, int stock, bool enabled, int sortOrder)
        {
            Name = name;
            Description = description;
            Icon = icon;
            PointPrice = pointPrice;
            FlashPrice = flashPrice;
            FlashStart = flashStart;
            FlashEnd = flashEnd;
            DurationHours = durationHours;
            Stock = stock;
            Enabled = enabled;
            SortOrder = sortOrder;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

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

        /// <summary>
        /// 归属商户 ID（v2.4.0 商家挂礼：null=平台自营权益；非空=该商家发布的实物礼品）。
        /// 金库类比"工会摆摊"：礼品兑换确认收货后的积分全额结算进该商户金库
        /// </summary>
        public Guid? OwnerMerchantId { get; private set; }

        /// <summary>
        /// 礼品审核态（v2.4.0）：0=平台商品不适用（免审），1=待审，2=已通过，3=已驳回。
        /// 商家礼品必须过"审核定档"——上架与否、积分价格都由平台审核时敲定，杜绝定向转移
        /// </summary>
        public int AuditState { get; private set; }

        /// <summary>审核备注（驳回原因/定档说明，商户可见）</summary>
        public string? AuditRemark { get; private set; }

        /// <summary>是否商家实物礼品（需审核+收货结算路径）</summary>
        public bool IsMerchantGift => OwnerMerchantId.HasValue && Category == MallItemCategory.Gift;

        /// <summary>商家礼品是否可售（审核通过 + 上架 + 有效）</summary>
        public bool IsGiftSellable => !IsMerchantGift || (AuditState == 2 && Enabled && IsActive);

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
        /// 商家申请挂礼（v2.4.0）：进待审态且默认下架——审核定档（平台定价+放行）后才可兑换。
        /// Key 由调用方生成（gift:{商户短码}:{时间戳} 之类全局唯一串），审核只改价不改 Key
        /// </summary>
        public static MallItem CreateGift(string key, Guid ownerMerchantId, string name,
            string description, string imageKey, int stock)
        {
            return new MallItem
            {
                Key = key,
                Name = name,
                Description = description,
                Icon = imageKey,
                Category = MallItemCategory.Gift,
                PointPrice = 0, // 价格由平台审核定档，申请时不生效
                Stock = stock,
                SortOrder = 100,
                OwnerMerchantId = ownerMerchantId,
                AuditState = 1,
                Enabled = false, // 待审即下架，通过后放行
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 审核通过并定档（v2.4.0）：平台统一定价（杜绝商家自定形成定向转移），放行上架
        /// </summary>
        public void PassGiftAudit(int pointPrice, string? remark)
        {
            PointPrice = pointPrice;
            AuditState = 2;
            AuditRemark = remark;
            Enabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>审核驳回（原因商户可见，可修改后重新申请）</summary>
        public void RejectGiftAudit(string reason)
        {
            AuditState = 3;
            AuditRemark = reason;
            Enabled = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>商家主动下架自己的礼品（存量订单不受影响）</summary>
        public void TakeOffShelfByOwner()
        {
            Enabled = false;
            UpdatedAt = DateTime.UtcNow;
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

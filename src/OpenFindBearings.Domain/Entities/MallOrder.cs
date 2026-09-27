using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商城订单（v2.3.0 商城虚拟权益）：一次积分兑换的完整凭据。
    /// 商品名/单价做快照——目录后续改价不影响历史订单展示与对账；
    /// 虚拟权益自动履约，正常路径在同一事务内 Pending→Fulfilled
    /// </summary>
    public class MallOrder : BaseEntity
    {
        /// <summary>兑换用户 ID</summary>
        public Guid UserId { get; private set; }

        /// <summary>商品 ID（目录行；快照字段另存，删目录不丢历史）</summary>
        public Guid ItemId { get; private set; }

        /// <summary>商品键快照</summary>
        public string ItemKey { get; private set; } = string.Empty;

        /// <summary>商品名快照</summary>
        public string ItemName { get; private set; } = string.Empty;

        /// <summary>实付积分快照（含闪购价）</summary>
        public int PointsSpent { get; private set; }

        /// <summary>订单状态</summary>
        public MallOrderStatus Status { get; private set; } = MallOrderStatus.Pending;

        /// <summary>履约目标引用（置顶卡=MerchantBearingId；其他类别可空）</summary>
        public Guid? TargetRef { get; private set; }

        /// <summary>履约备注/失败原因</summary>
        public string? Remark { get; private set; }

        /// <summary>履约时间（UTC）</summary>
        public DateTime? FulfilledAt { get; private set; }

        // ===== v2.4.0 商家实物礼品：收货与结算链（虚拟权益全部为空，互不干扰） =====

        /// <summary>发货状态：0=虚拟权益不适用，1=待发货，2=已发货，3=已收货（已结算），4=已退款</summary>
        public int ShipStatus { get; private set; }

        /// <summary>收货人姓名（实物兑换必填）</summary>
        public string? ReceiverName { get; private set; }

        /// <summary>收货人电话（实物兑换必填；同址同机月限单的反作弊维度）</summary>
        public string? ReceiverPhone { get; private set; }

        /// <summary>收货地址（实物兑换必填）</summary>
        public string? ReceiverAddress { get; private set; }

        /// <summary>物流单号（商家发货必填，M1 仅包邮无运费）</summary>
        public string? ShipTracking { get; private set; }

        /// <summary>发货时间（UTC，7 天自动确认收货的起算点）</summary>
        public DateTime? ShippedAt { get; private set; }

        /// <summary>确认收货时间（UTC）</summary>
        public DateTime? ReceivedAt { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected MallOrder() { }

        /// <summary>
        /// 创建订单（扣分成功后同事务写入；bizId 幂等由积分流水侧保证）。
        /// 实物礼品传入收货三件套：Status 直接 Fulfilled（托管扣款完成），物流走 ShipStatus
        /// </summary>
        public static MallOrder Create(Guid userId, MallItem item, int pointsSpent, Guid? targetRef,
            string? receiverName = null, string? receiverPhone = null, string? receiverAddress = null)
        {
            var isGift = item.IsMerchantGift;
            return new MallOrder
            {
                UserId = userId,
                ItemId = item.Id,
                ItemKey = item.Key,
                ItemName = item.Name,
                PointsSpent = pointsSpent,
                TargetRef = targetRef,
                Status = MallOrderStatus.Pending,
                ShipStatus = isGift ? 1 : 0,
                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ReceiverAddress = receiverAddress,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>标记履约成功（权益已生效）</summary>
        public void MarkFulfilled(string? remark = null)
        {
            Status = MallOrderStatus.Fulfilled;
            Remark = remark;
            FulfilledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>商家发货（仅待发货态可转）：记录物流单号，起算 7 天自动确认</summary>
        public void MarkShipped(string tracking)
        {
            if (ShipStatus != 1)
                throw new InvalidOperationException("订单当前不可发货");
            ShipStatus = 2;
            ShipTracking = tracking;
            ShippedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>确认收货（手动或 7 天自动）：结算前置态，随后由金库服务落结算流水</summary>
        public void MarkReceived(bool auto)
        {
            if (ShipStatus != 2)
                throw new InvalidOperationException("订单当前不可确认收货");
            ShipStatus = 3;
            ReceivedAt = DateTime.UtcNow;
            if (auto) Remark = "发货满 7 天自动确认收货";
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>标记履约失败（调用方须同事务退分）</summary>
        public void MarkFailed(string reason)
        {
            Status = MallOrderStatus.Failed;
            Remark = reason;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>退款（Admin 争议处理/冻结回滚：原路退分后标记；实物单同步终止物流态）</summary>
        public void MarkRefunded(string reason)
        {
            Status = MallOrderStatus.Refunded;
            Remark = reason;
            if (ShipStatus is 1 or 2) ShipStatus = 4;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

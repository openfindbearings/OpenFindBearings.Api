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

        /// <summary>EF 无参构造</summary>
        protected MallOrder() { }

        /// <summary>
        /// 创建订单（扣分成功后同事务写入；bizId 幂等由积分流水侧保证）
        /// </summary>
        public static MallOrder Create(Guid userId, MallItem item, int pointsSpent, Guid? targetRef)
        {
            return new MallOrder
            {
                UserId = userId,
                ItemId = item.Id,
                ItemKey = item.Key,
                ItemName = item.Name,
                PointsSpent = pointsSpent,
                TargetRef = targetRef,
                Status = MallOrderStatus.Pending,
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

        /// <summary>标记履约失败（调用方须同事务退分）</summary>
        public void MarkFailed(string reason)
        {
            Status = MallOrderStatus.Failed;
            Remark = reason;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>退款（Admin 争议处理/冻结回滚：原路退分后标记）</summary>
        public void MarkRefunded(string reason)
        {
            Status = MallOrderStatus.Refunded;
            Remark = reason;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

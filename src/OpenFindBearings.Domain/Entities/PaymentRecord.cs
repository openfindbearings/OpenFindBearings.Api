using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 付费记录
    /// </summary>
    public class PaymentRecord : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNo { get; private set; } = string.Empty;

        /// <summary>
        /// 金额（分）
        /// </summary>
        public int Amount { get; private set; }

        /// <summary>
        /// 支付方式（WeChat/Alipay）
        /// </summary>
        public string PaymentMethod { get; private set; } = string.Empty;

        /// <summary>
        /// 购买套餐
        /// </summary>
        public string Plan { get; private set; } = string.Empty;

        /// <summary>
        /// 支付状态（Pending/Success/Failed/Refunded）
        /// </summary>
        public string Status { get; private set; } = string.Empty;

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PaidAt { get; private set; }

        /// <summary>
        /// 第三方支付流水号
        /// </summary>
        public string? TransactionId { get; private set; }

        private PaymentRecord() { }

        public PaymentRecord(Guid userId, string orderNo, int amount, string paymentMethod, string plan)
        {
            UserId = userId;
            OrderNo = orderNo;
            Amount = amount;
            PaymentMethod = paymentMethod;
            Plan = plan;
            Status = "Pending";
        }

        public void MarkAsSuccess(string transactionId)
        {
            Status = "Success";
            TransactionId = transactionId;
            PaidAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void MarkAsFailed()
        {
            Status = "Failed";
            UpdateTimestamp();
        }

        public void MarkAsRefunded()
        {
            Status = "Refunded";
            UpdateTimestamp();
        }
    }
}

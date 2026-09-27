using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商家积分金库账户（v2.4.0 工会经济）：一商一户的商家维度积分余额。
    /// 工会类比=公会仓库：成员行为 trickle 与挂礼结算两条流入渠道都进这里；
    /// 仅商户管理员可在平台内消费（置顶卡等权益），永不折现、不可转给个人——
    /// 与 PointAccount（个人账本，键 UserId）平行存在、互不通融
    /// </summary>
    public class MerchantPointAccount : BaseEntity
    {
        /// <summary>所属商户 ID（唯一）</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>当前余额（>=0）</summary>
        public int Balance { get; private set; }

        /// <summary>累计获得（只增，明细页与运营统计）</summary>
        public int TotalEarned { get; private set; }

        /// <summary>累计消耗（只增）</summary>
        public int TotalSpent { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected MerchantPointAccount() { }

        /// <summary>
        /// 开户：余额 0
        /// </summary>
        public MerchantPointAccount(Guid merchantId)
        {
            MerchantId = merchantId;
            Balance = 0;
            TotalEarned = 0;
            TotalSpent = 0;
        }

        /// <summary>入账（trickle/结算共用）：amount 必须为正</summary>
        public void Credit(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("入账分值必须为正", nameof(amount));
            Balance += amount;
            TotalEarned += amount;
            UpdateTimestamp();
        }

        /// <summary>出账（消费/燃烧）：余额不足抛 InvalidOperationException，调用方兜底</summary>
        public void Debit(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("出账分值必须为正", nameof(amount));
            if (Balance < amount)
                throw new InvalidOperationException("商家金库余额不足");
            Balance -= amount;
            TotalSpent += amount;
            UpdateTimestamp();
        }
    }
}

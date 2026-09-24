using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 积分账户实体：一人一户的积分底座（合规三纪律：不可充值、不可提现、不可转让——
    /// 系统内不存在任何增加余额的外部资金入口，余额只能由各赚分规则产生）
    /// </summary>
    public class PointAccount : BaseEntity
    {
        /// <summary>所属用户 ID（唯一）</summary>
        public Guid UserId { get; private set; }

        /// <summary>当前余额（恒 >=0，扣减守卫在 Debit 内）</summary>
        public int Balance { get; private set; }

        /// <summary>累计获得（只增，供明细页与运营统计）</summary>
        public int TotalEarned { get; private set; }

        /// <summary>累计消耗（只增）</summary>
        public int TotalSpent { get; private set; }

        /// <summary>最近一次签到日期（UTC 日期，阶梯连续判定用；非签到动作为 null）</summary>
        public DateTime? LastCheckinDate { get; private set; }

        /// <summary>当前连续签到天数（签到日=昨天则 +1，中断则重置 1；阶梯取档用）</summary>
        public int ConsecutiveCheckinDays { get; private set; }

        /// <summary>EF 构造函数</summary>
        protected PointAccount() { }

        /// <summary>
        /// 开户：余额 0，创建即生效
        /// </summary>
        /// <param name="userId">所属用户 ID</param>
        public PointAccount(Guid userId)
        {
            UserId = userId;
            Balance = 0;
            TotalEarned = 0;
            TotalSpent = 0;
        }

        /// <summary>
        /// 入账：余额与累计获得同增（amount 必须为正）
        /// </summary>
        /// <param name="amount">入账分值</param>
        public void Credit(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("入账分值必须为正", nameof(amount));
            Balance += amount;
            TotalEarned += amount;
            UpdateTimestamp();
        }

        /// <summary>
        /// 出账：余额减、累计消耗增；余额不足抛业务异常（扣分场景守卫）
        /// </summary>
        /// <param name="amount">出账分值（正数）</param>
        public void Debit(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("出账分值必须为正", nameof(amount));
            if (Balance < amount)
                throw new InvalidOperationException($"积分余额不足：当前 {Balance}，需要 {amount}");
            Balance -= amount;
            TotalSpent += amount;
            UpdateTimestamp();
        }

        /// <summary>
        /// 记录签到并推进连续天数：签到日恰为昨天则连击 +1，否则重置为 1
        /// 改动说明：签到成功与 LastCheckinDate/连击数更新同事务，防阶梯跳变
        /// </summary>
        /// <param name="date">本次签到的 UTC 日期</param>
        /// <returns>本次签到后的连续天数（阶梯取档用）</returns>
        public int MarkCheckedIn(DateTime date)
        {
            var today = date.Date;
            if (LastCheckinDate.HasValue && LastCheckinDate.Value.Date == today)
                return ConsecutiveCheckinDays; // 同日重复调用不推进（幂等守卫在服务层，此为兜底）

            ConsecutiveCheckinDays = LastCheckinDate.HasValue && LastCheckinDate.Value.Date == today.AddDays(-1)
                ? ConsecutiveCheckinDays + 1
                : 1;
            LastCheckinDate = today;
            UpdateTimestamp();
            return ConsecutiveCheckinDays;
        }
    }
}

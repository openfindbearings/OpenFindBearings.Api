using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商家金库流水（v2.4.0 工会经济）：与个人 PointTransaction 平行的商户维度账本。
    /// BizId 部分唯一索引做幂等（trickle:{源流水}:{商户}、settle:{订单}、burn:{商户}:{事件}），
    /// 上限统计（日/月）按 GrantType 汇总，不引入第二套规则表——金库参数走 SystemConfig
    /// </summary>
    public class MerchantPointTransaction : BaseEntity
    {
        /// <summary>收入方向</summary>
        public const int DirectionCredit = 1;
        /// <summary>支出方向</summary>
        public const int DirectionDebit = 2;

        /// <summary>成员行为微量上供（trickle）</summary>
        public const string TypeMemberTrickle = "member_trickle";
        /// <summary>挂礼兑换确认收货后结算</summary>
        public const string TypeGiftSettlement = "gift_settlement";
        /// <summary>金库消费（置顶卡等平台权益）</summary>
        public const string TypeTreasurySpend = "treasury_spend";
        /// <summary>关店/解除归属燃烧</summary>
        public const string TypeBurn = "treasury_burn";

        /// <summary>集体任务达成奖励（v2.6.0 M3）</summary>
        public const string TypeGuildTaskReward = "guild_task_reward";

        /// <summary>所属商户 ID</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>方向（1 收 / 2 支）</summary>
        public int Direction { get; private set; }

        /// <summary>场景类型（本类常量）</summary>
        public string GrantType { get; private set; } = string.Empty;

        /// <summary>分值（正数）</summary>
        public int Amount { get; private set; }

        /// <summary>记账后余额快照</summary>
        public int BalanceAfter { get; private set; }

        /// <summary>幂等键（可空；部分唯一索引仅约束非空值）</summary>
        public string? BizId { get; private set; }

        /// <summary>明细文案（用户/管理员可读）</summary>
        public string? Remark { get; private set; }

        /// <summary>EF 无参构造</summary>
        protected MerchantPointTransaction() { }

        /// <summary>
        /// 记一笔流水（余额快照由调用方在 Credit/Debit 之后传入）
        /// </summary>
        public MerchantPointTransaction(Guid merchantId, int direction, string grantType,
            int amount, int balanceAfter, string? bizId, string? remark)
        {
            MerchantId = merchantId;
            Direction = direction;
            GrantType = grantType;
            Amount = amount;
            BalanceAfter = balanceAfter;
            BizId = bizId;
            Remark = remark;
        }
    }
}

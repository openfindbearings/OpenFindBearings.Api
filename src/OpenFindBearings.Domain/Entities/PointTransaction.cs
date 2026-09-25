using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 积分流水实体：每笔增减一行，账本唯一事实源。
    /// BizId 唯一索引承担幂等（同一业务动作重复发放直接冲突跳过），
    /// BalanceAfter 快照使对账可线性重放，过期字段本期预留不启用
    /// </summary>
    public class PointTransaction : BaseEntity
    {
        /// <summary>方向：入账</summary>
        public const int DirectionCredit = 1;
        /// <summary>方向：出账</summary>
        public const int DirectionDebit = 2;

        /// <summary>赚分动作类型：每日登录</summary>
        public const string TypeDailyLogin = "daily_login";
        /// <summary>赚分动作类型：每日签到（阶梯）</summary>
        public const string TypeDailyCheckin = "daily_checkin";
        /// <summary>赚分动作类型：新用户注册一次性奖励</summary>
        public const string TypeRegisterBonus = "register_bonus";
        /// <summary>赚分动作类型：纠错被采纳</summary>
        public const string TypeCorrectionAdopted = "correction_adopted";
        /// <summary>赚分动作类型：商户入驻审核通过（一次性）</summary>
        public const string TypeMerchantApproved = "merchant_approved";
        /// <summary>赚分动作类型：商户资料完善度首次达标（一次性，bizId 绑信用代码防删店重入驻循环）</summary>
        public const string TypeMerchantProfileComplete = "merchant_profile_complete";
        /// <summary>赚分动作类型：商户首件商品上架（一次性，bizId 绑信用代码）</summary>
        public const string TypeMerchantFirstProduct = "merchant_first_product";

        /// <summary>所属用户 ID</summary>
        public Guid UserId { get; private set; }

        /// <summary>方向（Direction* 常量）</summary>
        public int Direction { get; private set; }

        /// <summary>动作类型（Type* 常量或扣分场景串）</summary>
        public string GrantType { get; private set; } = string.Empty;

        /// <summary>分值（恒正，方向由 Direction 表达）</summary>
        public int Amount { get; private set; }

        /// <summary>本笔后余额快照</summary>
        public int BalanceAfter { get; private set; }

        /// <summary>幂等键（如 daily_login:{userId}:{yyyyMMdd}），唯一索引防重复发放</summary>
        public string? BizId { get; private set; }

        /// <summary>备注（明细页展示，如"连续第 3 天"）</summary>
        public string? Remark { get; private set; }

        /// <summary>过期时间（本期预留不启用，按年过期 Job 后置）</summary>
        public DateTime? ExpireAt { get; private set; }

        /// <summary>EF 构造函数</summary>
        protected PointTransaction() { }

        /// <summary>
        /// 新建流水：创建即带余额快照
        /// </summary>
        /// <param name="userId">所属用户</param>
        /// <param name="direction">方向（Direction*）</param>
        /// <param name="grantType">动作类型</param>
        /// <param name="amount">分值（正数）</param>
        /// <param name="balanceAfter">本笔后余额</param>
        /// <param name="bizId">幂等键（可空）</param>
        /// <param name="remark">备注（可空）</param>
        public PointTransaction(Guid userId, int direction, string grantType, int amount,
            int balanceAfter, string? bizId = null, string? remark = null)
        {
            UserId = userId;
            Direction = direction;
            GrantType = grantType;
            Amount = amount;
            BalanceAfter = balanceAfter;
            BizId = bizId;
            Remark = remark;
        }
    }
}

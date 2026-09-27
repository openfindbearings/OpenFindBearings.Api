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
        // 改动说明（v2.1.0 成就子系统）：成就解锁一次性可花积分甜头（小额，bizId=ach:{key}:{owner} 幂等）
        public const string TypeAchievementUnlock = "achievement_unlock";
        // 改动说明（v2.6.0 M3 商家集体任务）：集体任务达标后全体在职成员各得一笔（bizId 含任务/商家/周期/用户四段幂等，分值由任务定义 amountOverride 覆盖）
        public const string TypeMerchantTask = "merchant_task";

        // 改动说明（v2.7.0 G2 每日任务板三件套）：签到 + 纠错采纳 + 寻货应答三项当日全完成额外 +30（bizId=每日键幂等）
        public const string TypeDailyCombo = "daily_combo";

        /// <summary>商城兑换扣分场景（v2.3.0 商城虚拟权益，DeductAsync 的 sceneType）</summary>
        public const string TypeMallRedeem = "mall_redeem";

        /// <summary>商城兑换失败退分场景（v2.3.0，履约异常原路退回，GrantAsync 的 grantType 不走规则表——直接走退款专用路径）</summary>
        public const string TypeMallRefund = "mall_refund";

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

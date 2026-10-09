using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商家金库服务接口（v2.4.0 商家经济）。
    /// 商家类比"商家仓库"的单一记账入口：成员 trickle、挂礼结算、金库消费、终局燃烧。
    /// 与个人 PointsService 平行——两个账本之间永远没有直接转账路径（合规红线），
    /// 桥只有两条：成员赚分按比例进仓库（单向被动），兑换仓库主人的礼品后结算入仓库（真实交易）
    /// </summary>
    public interface IMerchantPointsService
    {
        /// <summary>金库账户（可空=未开户，首笔入账懒开户）</summary>
        Task<MerchantPointAccount?> GetAccountAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>金库流水分页（时间倒序）</summary>
        Task<(List<MerchantPointTransaction> Items, int Total)> GetTransactionsAsync(
            Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 成员合格赚分的微量上供：仅白名单场景（审核/真实交易类，排除登录/签到/注册等被动项）；
        /// 总额=floor(成员所得×比例)，平分该成员全部在职商户，余数给最早加入的一家（M2 起改最高等级家）；
        /// 每家再受日顶/月顶约束。bizId=trickle:{来源流水}:{商户} 幂等。
        /// 由 PointsService 在发放成功后以独立作用域调用，本方法吞掉一切异常（金库不反噬赚分主流程）
        /// </summary>
        Task TrickleForEarningAsync(Guid userId, string grantType, int memberAmount, string? sourceBizId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 挂礼订单结算入发布商户金库（确认收货后与订单状态同批提交）。
        /// bizId=settle:{订单} 幂等；月顶超额部分不结算（留在平台侧），返回实际结算额
        /// </summary>
        Task<int> SettleOrderAsync(MallOrder order, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 金库消费记账（商户管理员购平台权益）：**只挂账不提交**，与订单/权益同批 SaveChanges 保原子；
        /// 余额不足抛 InvalidOperationException 由调用方转 400。返回 false=幂等命中（同笔消费勿重复履约）
        /// </summary>
        Task<bool> SpendAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default);

        /// <summary>关店/解除归属燃烧：余额清零记 treasury_burn 流水（与释放动作同事务）</summary>
        Task BurnOnReleaseAsync(Guid merchantId, string bizId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 集体任务达成奖励入账（v2.6.0 M3）：只挂账不提交，与完成台账同批 SaveChanges 保原子；bizId 幂等
        /// </summary>
        Task RewardTreasuryAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 商家等级升档礼入账（v2.12.0 等级玩法）：升到 Lv2/3/4 一次性入金库商家金。
        /// bizId=gradeup:{merchantId}:{rank} 幂等——每商户每档终身一次，复升同档不重发（防上下架刷金库）；
        /// 独立提交、吞失败（升档礼是附属账本，绝不反噬等级重算与业务主流程）
        /// </summary>
        Task GrantGradeUpBonusAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default);
    }
}

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 工会等级服务接口（v2.5.0 工会经济）：等级重算与"成员最高等级工会"解析。
    /// 等级是工会 buff 的唯一输入（无主工会概念——buff 取全部在职商户中等级最高的一家，
    /// 对应定案"养多家公司至少有一家在干活"）
    /// </summary>
    public interface IMerchantGradeService
    {
        /// <summary>
        /// 商户等级重算（认证/在售变化/挂礼结算/释放等事件后调用）：
        /// Standard=Active 即得；Verified=认证；Premium=认证+在售达阈值；Gold=Premium+金库累计达阈值。
        /// 吞一切异常（等级是附属计算值，绝不反噬主业务事务）
        /// </summary>
        Task RecomputeAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析成员的"最佳工会"：全部在职 Active 商户中等级最高的一家（平级取最早加入），
        /// 无归属返回 null（散人）
        /// </summary>
        Task<MemberGuildInfo?> GetBestForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>成员最佳工会信息（buff 展示与计算输入）</summary>
    /// <param name="MerchantId">商户 ID</param>
    /// <param name="MerchantName">商户名</param>
    /// <param name="Grade">等级（1 入驻/3 认证/2 活跃/4 金牌，数值非单调语义见枚举注释）</param>
    public record MemberGuildInfo(Guid MerchantId, string MerchantName, int Grade);
}

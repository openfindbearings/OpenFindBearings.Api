using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 寻货额度判定结果（v1.35.0）：Free=免费额度内；NeedPoints=需扣积分（附单价）；
    /// Rejected=触达硬上限（积分也买不动，防无限制刷）
    /// </summary>
    internal enum QuotaDecision
    {
        Free,
        NeedPoints,
        Rejected
    }

    /// <summary>
    /// 寻货发布/应答共用额度评估（v1.35.0）：免费额度（SystemConfig 可配）→ 积分加量
    /// （规则表 amount=单价、DailyLimit=硬上限）→ 拒绝。发布与应答两处同构逻辑收口于此，
    /// 防复制漂移
    /// </summary>
    internal static class SourcingQuotaHelper
    {
        /// <summary>
        /// 评估今日额度档位：todayCount 已达免费线且请求未带积分确认 → NeedPoints；
        /// 达硬上限 → Rejected；否则 Free
        /// </summary>
        /// <param name="todayCount">今日已用次数</param>
        /// <param name="freeLimit">免费额度（SystemConfig）</param>
        /// <param name="hardLimit">硬上限（规则表 DailyLimit，0=不限）</param>
        /// <param name="usePointsConfirmed">用户是否已确认花积分</param>
        /// <param name="pointPrice">积分单价（规则表 Amount）</param>
        /// <returns>档位与需扣分值（NeedPoints 时 pointPrice 用于前端展示"花 X 积分"）</returns>
        public static (QuotaDecision Decision, int PointsCost) Evaluate(
            int todayCount, int freeLimit, int hardLimit, bool usePointsConfirmed, int pointPrice)
        {
            if (todayCount < freeLimit)
                return (QuotaDecision.Free, 0);
            if (hardLimit > 0 && todayCount >= hardLimit)
                return (QuotaDecision.Rejected, 0);
            if (!usePointsConfirmed)
                return (QuotaDecision.NeedPoints, pointPrice);
            return (QuotaDecision.Free, pointPrice); // 确认花积分：由调用方先扣再走 Free 语义
        }
    }

    /// <summary>
    /// 读取寻货免费额度配置（缺省兜底：发布 3 条/应答 20 条）
    /// </summary>
    public static class SourcingConfigReader
    {
        /// <summary>
        /// 取整型配置值（仓储内部含类型转换与默认回退；负数视为无效回退默认）
        /// </summary>
        /// <param name="configRepository">系统配置仓储</param>
        /// <param name="key">配置键（Sourcing.FreePublishPerDay / Sourcing.FreeRespondPerDay）</param>
        /// <param name="fallback">配置缺失时的默认值</param>
        public static async Task<int> GetIntAsync(ISystemConfigRepository configRepository, string key, int fallback)
        {
            var parsed = await configRepository.GetValueAsync<int?>(key, null);
            return parsed.HasValue && parsed.Value >= 0 ? parsed.Value : fallback;
        }
    }
}

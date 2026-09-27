using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 工会集体任务服务接口（v2.6.0 M3 集体任务与排行）：
    /// 帮派 raid 类比——全公会共同推进周期目标，达标即全员/金库获奖，
    /// 周期防重由完成台账唯一键保证；排行榜按月金库入账衡量"工会实力"
    /// </summary>
    public interface IGuildTaskService
    {
        /// <summary>成员视角：当前商户的启用任务 + 本周期进度 + 完成态</summary>
        Task<List<GuildTaskProgress>> GetTasksForMerchantAsync(Guid merchantId,
            CancellationToken cancellationToken = default);

        /// <summary>结算扫描（Job 调用）：逐 Active 商户 × 启用任务，达标即记账发奖；返回本轮结算笔数</summary>
        Task<int> RunSettlementSweepAsync(CancellationToken cancellationToken = default);

        /// <summary>工会月榜：本月金库入账 TOP N（附商户名/等级；myMerchantId 用于高亮本工会）</summary>
        Task<GuildRankingResult> GetMonthlyRankingAsync(Guid? myMerchantId, CancellationToken cancellationToken = default);
    }

    /// <summary>集体任务进度视图（成员可见文案 + 进度条数据）</summary>
    /// <param name="TaskKey">任务键</param>
    /// <param name="Name">任务名</param>
    /// <param name="Description">描述</param>
    /// <param name="Target">目标值</param>
    /// <param name="Current">本周期实际值</param>
    /// <param name="Period">1 周 / 2 月</param>
    /// <param name="RewardType">1 全体成员 / 2 金库</param>
    /// <param name="RewardAmount">奖励分值</param>
    /// <param name="Done">本周期是否已达成结算</param>
    public record GuildTaskProgress(string TaskKey, string Name, string Description,
        int Target, int Current, int Period, int RewardType, int RewardAmount, bool Done);

    /// <summary>排行榜行</summary>
    public record GuildRankRow(int Rank, Guid MerchantId, string MerchantName, string GradeDisplay, int Total);

    /// <summary>排行榜结果：TOP 榜 + 本工会（可能不在榜内，单独回显名次）</summary>
    public record GuildRankingResult(List<GuildRankRow> Top, GuildRankRow? Mine, string PeriodKey);
}

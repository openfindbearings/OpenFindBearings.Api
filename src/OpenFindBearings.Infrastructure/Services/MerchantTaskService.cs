using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家集体任务服务实现（v2.6.0 M3）：商家 raid 类比的结算与展示中枢。
    /// 指标聚合全部复用既有仓储查询（corrections 走个人流水计数、treasury 走金库流水、products 走商品明细），
    /// 本服务只做"周期窗口计算 + 达标裁决 + 防重结算 + 发奖编排"。
    /// 结算纪律：金库奖励=台账与入账同批 SaveChanges 保原子；成员奖励=台账先占坑（防并发双结算），
    ///   逐人发放走 PointsService 幂等 bizId（各自独立提交、失败自吞），两者都靠唯一索引兜底并发
    /// </summary>
    public class MerchantTaskService : IMerchantTaskService
    {
        /// <summary>排行榜展示条数</summary>
        private const int RankingDisplayTop = 20;
        /// <summary>排行榜取数深度（"本商家"回显兜底：原型规模商户数远小于此，出 100 名按未上榜处理）</summary>
        private const int RankingFetchDepth = 100;

        private readonly ILogger<MerchantTaskService> _logger;
        private readonly IMerchantTaskRepository _tasks;
        private readonly IMerchantRepository _merchants;
        private readonly IMerchantMemberRepository _members;
        private readonly IPointTransactionRepository _pointTxs;
        private readonly IMerchantPointTransactionRepository _treasuryTxs;
        private readonly IMerchantBearingRepository _bearings;
        private readonly IPointsService _points;
        private readonly IMerchantPointsService _treasury;
        private readonly IUnitOfWork _unitOfWork;
        // 结算撞唯一索引后清理 ChangeTracker 用（防脏行连累同作用域后续保存）
        private readonly ApplicationDbContext _context;

        /// <summary>构造：任务/商户/成员/流水/金库/商品仓储 + 个人与金库发奖服务 + 工作单元</summary>
        public MerchantTaskService(
            ILogger<MerchantTaskService> logger,
            IMerchantTaskRepository tasks,
            IMerchantRepository merchants,
            IMerchantMemberRepository members,
            IPointTransactionRepository pointTxs,
            IMerchantPointTransactionRepository treasuryTxs,
            IMerchantBearingRepository bearings,
            IPointsService points,
            IMerchantPointsService treasury,
            IUnitOfWork unitOfWork,
            ApplicationDbContext context)
        {
            _logger = logger;
            _tasks = tasks;
            _merchants = merchants;
            _members = members;
            _pointTxs = pointTxs;
            _treasuryTxs = treasuryTxs;
            _bearings = bearings;
            _points = points;
            _treasury = treasury;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<List<MerchantTaskProgress>> GetTasksForMerchantAsync(Guid merchantId,
            CancellationToken cancellationToken = default)
        {
            var defs = await _tasks.GetEnabledAsync(cancellationToken);
            if (defs.Count == 0)
                return new List<MerchantTaskProgress>();

            var weekly = CurrentWeeklyWindow();
            var monthly = CurrentMonthlyWindow();

            // 两个周期的已完成台账各查一次（同周期任务共用）
            var doneWeek = await _tasks.GetCompletedTaskKeysAsync(merchantId, weekly.Key, cancellationToken);
            var doneMonth = await _tasks.GetCompletedTaskKeysAsync(merchantId, monthly.Key, cancellationToken);

            // corrections 指标才需要成员名单，存在该类任务时才取一次
            List<Guid>? memberIds = null;
            if (defs.Any(d => d.MetricKey == MerchantTaskDefinition.MetricCorrections
                              && !((d.Period == MerchantTaskDefinition.PeriodMonthly ? doneMonth : doneWeek)
                                  .Contains(d.TaskKey, StringComparer.Ordinal))))
            {
                memberIds = (await _members.GetActiveByMerchantAsync(merchantId, cancellationToken))
                    .Select(m => m.UserId).ToList();
            }

            var result = new List<MerchantTaskProgress>(defs.Count);
            foreach (var def in defs)
            {
                var (periodKey, sinceUtc) = def.Period == MerchantTaskDefinition.PeriodMonthly ? monthly : weekly;
                var done = (def.Period == MerchantTaskDefinition.PeriodMonthly ? doneMonth : doneWeek)
                    .Contains(def.TaskKey, StringComparer.Ordinal);
                // 已达成任务的进度钉在目标值（窗口内指标只增不减，钉满显示最直观）
                var current = done ? def.TargetValue
                    : await MeasureAsync(def, merchantId, sinceUtc, memberIds, cancellationToken);
                result.Add(new MerchantTaskProgress(def.TaskKey, def.Name, def.Description,
                    def.TargetValue, Math.Min(current, def.TargetValue), def.Period, def.RewardType,
                    def.RewardAmount, done));
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task<int> RunSettlementSweepAsync(CancellationToken cancellationToken = default)
        {
            var defs = await _tasks.GetEnabledAsync(cancellationToken);
            if (defs.Count == 0)
                return 0;

            var merchantIds = await _merchants.GetActiveIdsAsync(cancellationToken);
            var weekly = CurrentWeeklyWindow();
            var monthly = CurrentMonthlyWindow();
            var settled = 0;

            foreach (var merchantId in merchantIds)
            {
                List<Guid>? memberIds = null;
                foreach (var def in defs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (periodKey, sinceUtc) = def.Period == MerchantTaskDefinition.PeriodMonthly ? monthly : weekly;

                    // 台账判重：该周期已结算直接跳过（多数轮次在此短路，成本一次小查询）
                    var doneKeys = await _tasks.GetCompletedTaskKeysAsync(merchantId, periodKey, cancellationToken);
                    if (doneKeys.Contains(def.TaskKey, StringComparer.Ordinal))
                        continue;

                    var value = await MeasureAsync(def, merchantId, sinceUtc,
                        def.MetricKey == MerchantTaskDefinition.MetricCorrections
                            ? (memberIds ??= (await _members.GetActiveByMerchantAsync(merchantId, cancellationToken))
                                .Select(m => m.UserId).ToList())
                            : memberIds, cancellationToken);
                    if (value < def.TargetValue)
                        continue;

                    if (def.RewardType == MerchantTaskDefinition.RewardTreasury)
                        settled += await SettleTreasuryAsync(def, merchantId, periodKey, value, cancellationToken) ? 1 : 0;
                    else
                        settled += await SettleMembersAsync(def, merchantId, periodKey, value, cancellationToken) ? 1 : 0;
                }
            }
            return settled;
        }

        /// <inheritdoc/>
        public async Task<MerchantRankingResult> GetMonthlyRankingAsync(Guid? myMerchantId,
            CancellationToken cancellationToken = default)
        {
            var monthly = CurrentMonthlyWindow();
            var top = await _treasuryTxs.GetTopMerchantsCreditAsync(monthly.UtcStart, RankingFetchDepth, cancellationToken);

            // 商户名/等级批量补齐（一次查询，删除的商户行跳过展示）
            var briefs = (await _merchants.GetByIdsAsync(top.Select(t => t.MerchantId), cancellationToken))
                .ToDictionary(m => m.Id);
            var rows = new List<MerchantRankRow>(top.Count);
            for (var i = 0; i < top.Count; i++)
            {
                if (!briefs.TryGetValue(top[i].MerchantId, out var merchant))
                    continue;
                rows.Add(new MerchantRankRow(i + 1, merchant.Id, merchant.Name, merchant.GetGradeDisplayName(), top[i].Total));
            }

            MerchantRankRow? mine = null;
            if (myMerchantId.HasValue)
            {
                mine = rows.FirstOrDefault(r => r.MerchantId == myMerchantId.Value);
                if (mine == null)
                {
                    // 未进前 100：单独回显总额，名次按 0（前端展示"未上榜"）
                    var total = await _treasuryTxs.SumCreditAnySinceAsync(myMerchantId.Value, monthly.UtcStart, cancellationToken);
                    var merchant = await _merchants.GetByIdAsync(myMerchantId.Value, cancellationToken);
                    if (merchant != null)
                        mine = new MerchantRankRow(0, merchant.Id, merchant.Name, merchant.GetGradeDisplayName(), total);
                }
            }
            return new MerchantRankingResult(rows.Take(RankingDisplayTop).ToList(), mine, monthly.Key);
        }

        /// <summary>
        /// 金库奖励结算：台账 + 金库入账同批 SaveChanges（原子）。
        /// 返回 false=并发撞唯一索引（另一轮/另一 Pod 已结算），清 tracker 防脏行连累后续
        /// </summary>
        private async Task<bool> SettleTreasuryAsync(MerchantTaskDefinition def, Guid merchantId,
            string periodKey, int value, CancellationToken ct)
        {
            try
            {
                await _tasks.AddCompletionAsync(new MerchantTaskCompletion(def.TaskKey, merchantId, periodKey, value), ct);
                await _treasury.RewardTreasuryAsync(merchantId, def.RewardAmount,
                    $"merchanttask:{def.TaskKey}:{merchantId:N}:{periodKey}", $"集体任务达成：{def.Name}", ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("集体任务金库结算: Task={TaskKey}, Merchant={MerchantId}, Period={Period}, Reward={Reward}",
                    def.TaskKey, merchantId, periodKey, def.RewardAmount);
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        /// <summary>
        /// 成员奖励结算：台账先独立提交占坑（占坑失败=并发已结算，绝不发奖）；
        /// 逐人 GrantAsync 幂等 bizId 各自提交（PointsService 吞单人失败，不拖垮其余成员）
        /// </summary>
        private async Task<bool> SettleMembersAsync(MerchantTaskDefinition def, Guid merchantId,
            string periodKey, int value, CancellationToken ct)
        {
            try
            {
                await _tasks.AddCompletionAsync(new MerchantTaskCompletion(def.TaskKey, merchantId, periodKey, value), ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }

            var members = await _members.GetActiveByMerchantAsync(merchantId, ct);
            foreach (var member in members)
            {
                await _points.GrantAsync(member.UserId, PointTransaction.TypeMerchantTask,
                    $"merchanttask:{def.TaskKey}:{merchantId:N}:{periodKey}:{member.UserId:N}",
                    $"商家集体任务达成：{def.Name}", def.RewardAmount, ct);
            }
            _logger.LogInformation("集体任务成员奖励发放: Task={TaskKey}, Merchant={MerchantId}, Period={Period}, Members={Count}",
                def.TaskKey, merchantId, periodKey, members.Count);
            return true;
        }

        /// <summary>按任务指标键路由到对应仓储聚合查询；未知指标返回 0（宁不发奖不误发）</summary>
        private async Task<int> MeasureAsync(MerchantTaskDefinition def, Guid merchantId, DateTime sinceUtc,
            List<Guid>? memberIds, CancellationToken ct)
        {
            return def.MetricKey switch
            {
                // 成员纠错被采纳数：按在职成员集合统计本周期 correction_adopted 入账流水条数
                MerchantTaskDefinition.MetricCorrections =>
                    await _pointTxs.CountByUsersTypeSinceAsync(memberIds ?? new List<Guid>(),
                        PointTransaction.TypeCorrectionAdopted, sinceUtc, ct),
                // 金库入账总额：任意来源（trickle+结算+任务奖励）全部计入商家实力
                MerchantTaskDefinition.MetricTreasury =>
                    await _treasuryTxs.SumCreditAnySinceAsync(merchantId, sinceUtc, ct),
                // 新上架商品数：窗口内新建产品关联（含待审核，鼓励供给动作本身）
                MerchantTaskDefinition.MetricProducts =>
                    await _bearings.CountCreatedSinceAsync(merchantId, sinceUtc, ct),
                _ => 0
            };
        }

        /// <summary>当前业务周窗口：Key=周一 yyyyMMdd，UtcStart=周一零点业务日历换算的 UTC 时刻</summary>
        private static (string Key, DateTime UtcStart) CurrentWeeklyWindow()
        {
            var now = BusinessClock.Now;
            var monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            return (monday.ToString("yyyyMMdd"), DateTime.SpecifyKind(monday - BusinessClock.Offset, DateTimeKind.Utc));
        }

        /// <summary>当前业务月窗口：Key=yyyyMM，UtcStart=月首零点业务日历换算的 UTC 时刻（与金库月顶口径同款）</summary>
        private static (string Key, DateTime UtcStart) CurrentMonthlyWindow()
        {
            var now = BusinessClock.Now;
            var first = new DateTime(now.Year, now.Month, 1);
            return (first.ToString("yyyyMM"), DateTime.SpecifyKind(first - BusinessClock.Offset, DateTimeKind.Utc));
        }

    }
}

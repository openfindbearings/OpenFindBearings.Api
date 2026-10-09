using System.Text.Json;
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
    /// 积分服务实现（v1.32.0 积分底座）：规则表驱动、BizId 幂等、日上限防刷。
    /// 时序设计：赚分调用点均在业务事务提交后（事件订阅者）或无事务上下文（中间件/签到命令），
    ///   故与 NotificationService 同款——Add 后显式 SaveChanges 独立落库；
    ///   发放失败只记日志绝不抛出（积分是附属账本，不反噬业务主流程）；
    ///   扣减失败必须抛出（余额不足是用户可感知的业务结果）
    /// </summary>
    public class PointsService : IPointsService
    {
        private readonly ILogger<PointsService> _logger;
        private readonly IPointAccountRepository _accountRepository;
        private readonly IPointTransactionRepository _transactionRepository;
        private readonly IPointGrantRuleRepository _ruleRepository;
        // v1.34.0：一次性奖励认领台账（号/照维度不变量，注销清流水后仍防重）
        private readonly IPointRewardClaimRepository _claimRepository;
        private readonly IUnitOfWork _unitOfWork;
        // 改动说明（v2.3.1 登录修复）：SaveChanges 失败后需清理本次挂入的实体——
        // 吞异常但不清 tracker 会让脏行随请求 DbContext 外溢到业务写库（并行发放撞 23505 后登录 500 的根因）
        private readonly ApplicationDbContext _context;
        // 改动说明（v2.4.0 商家经济）：成员合格赚分后向所属商户金库微量上供（trickle），
        // 白名单与上限全部在金库服务内部裁决；发放成功后同上下文调用（同请求同库，无需新事务）
        private readonly IMerchantPointsService _merchantPoints;
        // 改动说明（v2.5.0 商家经济）：成员被动加成——签到/登录/纠错按"最佳商家"等级加成，
        // 在日上限截顶之前套用（加成结果仍受 DailyLimit 约束，防叠出无顶收益）
        private readonly IMerchantGradeService _merchantGrades;
        // 改动说明（v2.7.0 G2 三件套）：判定用户今日应答次数（寻货应答完成信号源）
        private readonly ISourcingResponseRepository _sourcingResponses;
        // 改动说明（v2.12.0 等级玩法）：段位档位表——每次入账后按累计获得积分跨档，
        // 补发终身一次的升档礼（幂等键 levelup:{userId}:{lv}）
        private readonly IPointLevelRepository _levelRepository;

        /// <summary>
        /// 构造：账户/流水/规则/台账/档位仓储 + 工作单元（独立提交用）+ 上下文（失败清理用）+ 金库服务（trickle 挂钩）+ 等级服务（buff）+ 应答仓储（三件套判定）
        /// </summary>
        public PointsService(
            ILogger<PointsService> logger,
            IPointAccountRepository accountRepository,
            IPointTransactionRepository transactionRepository,
            IPointGrantRuleRepository ruleRepository,
            IPointRewardClaimRepository claimRepository,
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            IMerchantPointsService merchantPoints,
            IMerchantGradeService grades,
            ISourcingResponseRepository sourcingResponses,
            IPointLevelRepository levelRepository)
        {
            _logger = logger;
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _ruleRepository = ruleRepository;
            _claimRepository = claimRepository;
            _unitOfWork = unitOfWork;
            _context = context;
            _merchantPoints = merchantPoints;
            _merchantGrades = grades;
            _sourcingResponses = sourcingResponses;
            _levelRepository = levelRepository;
        }

        /// <inheritdoc/>
        public async Task<int> GrantAsync(Guid userId, string grantType, string? bizId = null,
            string? remark = null, int? amountOverride = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var rule = await _ruleRepository.GetEnabledByTypeAsync(grantType, cancellationToken);
                if (rule == null)
                    return 0; // 规则停用/不存在：默认关闭，运营开关动态生效

                if (bizId != null && await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                    return 0; // 幂等：同动作重复发放

                // 改动说明（v2.1.0）：amountOverride 供平台内部定义分值场景（成就解锁甜头），
                // 缺省回退规则表 Amount；仍受日上限截断守卫
                var amount = amountOverride ?? rule.Amount;

                // 改动说明（v2.5.0 商家经济）：商家 buff 在截顶前套用——
                // 登录 Lv2+1、纠错 Lv2×1.1/Lv3×1.2/Lv4×1.25；其余场景原额返回零查询
                amount = await ApplyMerchantBuffAsync(userId, grantType, amount, cancellationToken);

                // 改动说明（v2.8.0 G1 暴击）：服务端 RNG 判定——传说优先（×5），其次双倍（×2），
                // 命中后仍受下方 DailyLimit 截断（金额不足时余量封顶为剩余额度）
                amount = RollCrit(rule, amount);

                if (rule.DailyLimit > 0)
                {
                    var todaySum = await _transactionRepository.SumTodayByTypeAsync(userId, grantType, cancellationToken);
                    if (todaySum >= rule.DailyLimit)
                        return 0;
                    amount = Math.Min(amount, rule.DailyLimit - todaySum); // 截断到剩余额度
                }
                if (amount <= 0)
                    return 0;

                await GrantCoreAsync(userId, grantType, amount, bizId, remark, cancellationToken);
                return amount;
            }
            catch (Exception ex)
            {
                // 发放失败不阻塞业务主流程（登录/审批/事件链路与积分解耦）
                _logger.LogWarning(ex, "积分发放失败: UserId={UserId}, Type={Type}, BizId={BizId}",
                    userId, grantType, bizId);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GrantOneTimeAsync(Guid userId, string grantType, string claimKey,
            string? remark = null, int? amountOverride = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // 先向台账原子占坑：键=手机号/信用代码等跨账号不变量。
                // 占到坑才发奖；流水 bizId 同步写同键做第二道幂等（同日双击/重放场景）
                var claimed = await _claimRepository.TryClaimAsync(claimKey, grantType, userId, cancellationToken);
                if (!claimed)
                    return 0; // 该号/照历史已领过：注销重注册/删店重入驻循环免疫
                return await GrantAsync(userId, grantType, claimKey, remark, amountOverride, cancellationToken);
            }
            catch (Exception ex)
            {
                // 与 GrantAsync 同纪律：一次性奖励失败不反噬业务主流程
                _logger.LogWarning(ex, "一次性积分发放失败: UserId={UserId}, Type={Type}, Key={Key}",
                    userId, grantType, claimKey);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeductAsync(Guid userId, string sceneType, int amount, string? bizId = null,
            string? remark = null, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
                throw new ArgumentException("扣减分值必须为正", nameof(amount));

            if (bizId != null && await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                return 0; // 幂等：同笔扣减重复提交

            var account = await _accountRepository.GetByUserIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException("积分账户不存在");

            account.Debit(amount); // 余额不足在此抛出（调用方转 400 给用户）
            await _accountRepository.UpdateAsync(account, cancellationToken);
            var tx = new PointTransaction(
                userId, PointTransaction.DirectionDebit, sceneType, amount, account.Balance, bizId, remark);
            await _transactionRepository.AddAsync(tx, cancellationToken);
            // 改动说明（v2.3.1 登录修复）：扣分同样可能撞 23505（并发同 requestId）或 xmin 并发异常，
            // 失败后清理 tracker 再抛——调用方（商城兑换/寻货超额）要接着走退款或返回错误，脏账不能外溢
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await CleanupFailedGrantAsync(account, tx, false, cancellationToken);
                throw;
            }

            _logger.LogInformation("积分扣减: UserId={UserId}, Scene={Scene}, Amount={Amount}", userId, sceneType, amount);
            return amount;
        }

        /// <inheritdoc/>
        public async Task<int> RefundAsync(Guid userId, int amount, string bizId, string? remark = null,
            string grantType = PointTransaction.TypeMallRefund, CancellationToken cancellationToken = default)
        {
            // 改动说明（v2.3.0 商城）：退款刻意不查规则表——退款额来自订单快照，
            // 规则被停用/改值都不该影响用户拿回自己的分；仍走 GrantCore 保持流水与余额一致
            try
            {
                if (amount <= 0)
                    return 0;
                if (await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                    return 0; // 幂等：同笔退款重复提交

                await GrantCoreAsync(userId, grantType, amount, bizId, remark, cancellationToken);
                _logger.LogInformation("积分退款: UserId={UserId}, Amount={Amount}, BizId={BizId}", userId, amount, bizId);
                return amount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "积分退款失败（需人工介入）: UserId={UserId}, BizId={BizId}", userId, bizId);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<CheckinResult> CheckinAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 改动说明（v1.36.1 日界修复）：签到日键与连签基准从 UTC 日改为 BusinessClock 北京日——
            // 北京时间早 8 点前的签到不再撞昨日 UTC 键导致漏发分；DB 写入仍纯 UTC 不变
            var today = BusinessClock.Today;
            var bizId = $"checkin:{userId:N}:{BusinessClock.DateKey}";
            if (await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                return new CheckinResult(0, 0, true);

            var rule = await _ruleRepository.GetEnabledByTypeAsync(PointTransaction.TypeDailyCheckin, cancellationToken);
            if (rule == null)
                return new CheckinResult(0, 0, true); // 签到口被运营停用

            var account = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
            var accountWasNew = account == null;
            account ??= new PointAccount(userId);
            // 改动说明（v2.12.0 等级玩法）：签到旁路 GrantCore 直连账本，跨档礼需在保存后手动结算；
            // 先留存入账前累计以便回传 leveledUp（签到响应带升级信号供前端 toast）
            var totalEarnedBefore = account.TotalEarned;

            // 阶梯：按连续天数取档（[2,3,4,5,5] 第 6 天起恒取末档 5）；无阶梯配置回退基础分值
            var streak = account.MarkCheckedIn(today);
            var amount = PickLadderAmount(rule.LadderJson, streak) ?? rule.Amount;
            // 改动说明（v2.5.0 商家经济）：签到加成为商家 Lv1~+1/Lv2+1/Lv3+2/Lv4+3（阶梯后叠加）
            try
            {
                var best = await _merchantGrades.GetBestForUserAsync(userId, cancellationToken);
                amount += MerchantBuffs.CheckinBonus(best?.Grade ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "签到商家 buff 查询失败（按无加成）: User={UserId}", userId);
            }

            // 改动说明（v2.8.0 G1 暴击）：签到命中暴击——传说 ×5 / 双倍 ×2，
            // 暴击倍数回传前端播动画；仍受下方 DailyLimit 截断（截断后倍数按实际发放缩水）
            var critMultiplier = 1;
            if (rule.DoubleChance > 0 || rule.LegendChance > 0)
            {
                var roll = Random.Shared.Next(1, 101);
                if (rule.LegendChance > 0 && roll <= rule.LegendChance)
                    critMultiplier = 5;
                else if (rule.DoubleChance > 0 && roll <= rule.DoubleChance + rule.LegendChance)
                    critMultiplier = 2;
                amount *= critMultiplier;
            }

            if (rule.DailyLimit > 0)
            {
                var todaySum = await _transactionRepository.SumTodayByTypeAsync(userId, PointTransaction.TypeDailyCheckin, cancellationToken);
                if (todaySum >= rule.DailyLimit)
                {
                    // 同日已签到（幂等命中，正常分支）；不重复暴击
                    return new CheckinResult(0, streak, true);
                }
                amount = Math.Min(amount, rule.DailyLimit - todaySum);
                if (amount <= 0)
                    return new CheckinResult(0, streak, true);
            }

            account.Credit(amount);
            if (accountWasNew)
                await _accountRepository.AddAsync(account, cancellationToken);
            else
                await _accountRepository.UpdateAsync(account, cancellationToken);

            // 改动说明（v2.3.1 登录修复）：双端同时点签到存在与 daily_login 同型的查插竞态，
            // 输者撞 23505 后必须清 tracker，否则连累同一请求的后续写库
            var tx = new PointTransaction(
                userId, PointTransaction.DirectionCredit, PointTransaction.TypeDailyCheckin,
                amount, account.Balance, bizId, $"连续第 {streak} 天");
            await _transactionRepository.AddAsync(tx, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await CleanupFailedGrantAsync(account, tx, accountWasNew, cancellationToken);
                throw;
            }

            // 改动说明（v2.12.0 等级玩法）：签到旁路不走 GrantCoreAsync 后置钩子，此处补结算升档礼；
            // 结算后按最终累计落档回传 level/leveledUp（升级礼入账可能再推高档，一并反映）
            await TryGrantLevelUpBonusesAsync(userId, account.TotalEarned, cancellationToken);
            var levels = await _levelRepository.GetEnabledAsync(cancellationToken);
            var levelBefore = ResolveLevel(levels, totalEarnedBefore);
            var levelAfter = ResolveLevel(levels, account.TotalEarned);
            var leveledUp = (levelAfter?.Level ?? 1) > (levelBefore?.Level ?? 1);
            return new CheckinResult(amount, streak, false, critMultiplier,
                levelAfter?.Level ?? 1, levelAfter?.Name ?? "倔强青铜", leveledUp);
        }

        /// <summary>
        /// 开户或取已有账户 → 入账 → 写流水 → 独立提交（GrantCore 失败由调用方吞）。
        /// 改动说明（v2.3.1 登录修复）：失败时必须清理 tracker——并行请求对同一 bizId 的
        /// "查在否→插入"存在竞态，输者 23505 后本事务已整体回滚，但失败实体若仍挂 Added/Modified，
        /// 会随请求 DbContext 外溢到登录 JIT 等业务写库（二次插入再抛 23505 → 登录 500 的根因）
        /// </summary>
        private async Task GrantCoreAsync(Guid userId, string grantType, int amount, string? bizId,
            string? remark, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
            var accountWasNew = account == null;
            if (account == null)
            {
                account = new PointAccount(userId);
                await _accountRepository.AddAsync(account, cancellationToken);
            }
            else
            {
                // 改动说明（xmin 并发令牌）：PointAccounts 映射 PG 系统列 xmin 为并发令牌。
                // 同一请求链（如成就"一键多规则"连续发放）对同一账户多次 Grant 时，
                // ChangeTracker 缓存的是上次 SaveChanges 提交前的旧 xmin，二次 UPDATE WHERE xmin
                // 不命中报 DbUpdateConcurrencyException（实测 expected 1 / affected 0）。
                // 写前 Reload 强制取最新令牌，保证每次入账都以当前 xmin 竞争，提交后内存值随之刷新
                await _context.Entry(account).ReloadAsync(cancellationToken);
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }

            account.Credit(amount);
            var tx = new PointTransaction(
                userId, PointTransaction.DirectionCredit, grantType, amount, account.Balance, bizId, remark);
            await _transactionRepository.AddAsync(tx, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await CleanupFailedGrantAsync(account, tx, accountWasNew, cancellationToken);
                throw;
            }

            // 改动说明（v2.4.0 商家经济）：入账成功后尝试向成员所属商户金库上供——
            // 白名单（审核/交易类）在金库服务内部校验，登录/签到/注册等被动项自动跳过；
            // 同上下文提交但金库自行吞失败，绝不反噬个人赚分
            await _merchantPoints.TrickleForEarningAsync(userId, grantType, amount, bizId, cancellationToken);

            // 改动说明（v2.12.0 等级玩法）：入账成功后结算段位跨档——凡已达档且未领过的
            // 升档礼一次性补发（跳档合并到账）；吞失败不反噬本次赚分主流程
            await TryGrantLevelUpBonusesAsync(userId, account.TotalEarned, cancellationToken);
        }

        /// <summary>
        /// 段位升档礼结算（v2.12.0 等级玩法）：按累计获得积分取全部已达档（Lv2 起、Bonus>0），
        /// 未领过的档逐个走一次性发放——bizId/台账键 levelup:{userId}:{lv} 双保险，每用户每档终身一次。
        /// 升档礼入账本身又触发本方法（经 GrantCoreAsync 后置钩子），已领档被幂等拦截、
        /// 新跨档顺路补发，档位有限故必然收敛；吞一切异常，绝不影响赚分主链路
        /// </summary>
        private async Task TryGrantLevelUpBonusesAsync(Guid userId, int totalEarned, CancellationToken cancellationToken)
        {
            try
            {
                var levels = await _levelRepository.GetEnabledAsync(cancellationToken);
                foreach (var level in levels
                    .Where(l => l.Level >= 2 && l.LevelUpBonus > 0 && l.MinTotalEarned <= totalEarned)
                    .OrderBy(l => l.Level))
                {
                    var claimKey = $"levelup:{userId:N}:{level.Level}";
                    if (await _transactionRepository.ExistsBizIdAsync(claimKey, cancellationToken))
                        continue; // 该档升档礼终身已领：跳过（正常态由流水唯一键兜底，此处省一次写冲突）
                    await GrantOneTimeAsync(userId, PointTransaction.TypeLevelUpBonus, claimKey,
                        $"段位升档礼：{level.Name}", level.LevelUpBonus, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "段位升档礼结算失败（不反噬赚分）: User={UserId}", userId);
            }
        }

        /// <summary>
        /// 按累计获得积分落档（v2.12.0）：取最后一个阈值达到的启用档；空表兜底 Lv1 初档语义由调用方处理
        /// </summary>
        private static PointLevel? ResolveLevel(List<PointLevel> levels, int totalEarned)
            => levels.LastOrDefault(l => l.MinTotalEarned <= totalEarned);

        /// <summary>
        /// SaveChanges 失败后清理挂账实体：流水行一律 Detached；新建账户 Detached（DB 无行），
        /// 已有账户 Reload 还原内存余额与 xmin 令牌，保证同一请求后续业务写库不被污染
        /// </summary>
        private async Task CleanupFailedGrantAsync(PointAccount account, PointTransaction tx, bool accountWasNew,
            CancellationToken cancellationToken)
        {
            _context.Entry(tx).State = EntityState.Detached;
            if (accountWasNew)
                _context.Entry(account).State = EntityState.Detached;
            else
                await _context.Entry(account).ReloadAsync(cancellationToken);
        }

        /// <summary>
        /// 商家 buff 套用（v2.5.0）：仅白名单场景触发最佳商家查询（一次读，低频可接受）；
        /// 查询异常时按无 buff 处理——加成是锦上添花，绝不阻断发放
        /// </summary>
        private async Task<int> ApplyMerchantBuffAsync(Guid userId, string grantType, int amount, CancellationToken ct)
        {
            if (grantType != PointTransaction.TypeDailyLogin && grantType != PointTransaction.TypeCorrectionAdopted)
                return amount;
            try
            {
                var best = await _merchantGrades.GetBestForUserAsync(userId, ct);
                if (best == null)
                    return amount;
                return grantType switch
                {
                    PointTransaction.TypeDailyLogin => amount + MerchantBuffs.LoginBonus(best.Grade),
                    PointTransaction.TypeCorrectionAdopted => MerchantBuffs.ApplyCorrectionBonus(amount, best.Grade),
                    _ => amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "商家 buff 查询失败（按无加成发放）: User={UserId}", userId);
                return amount;
            }
        }

        /// <inheritdoc/>
        public async Task<int> TryGrantDailyComboAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 改动说明（v2.7.0 G2 每日任务板三件套）：三处触发点（签到成功/纠错采纳/应答新增）
            // 都调用本方法，无论哪项最后完成都会判定补发——bizId=daily_combo:{userId}:{业务日键} 幂等，
            // 重复触发自然跳过。判定口径：签到与纠错看今日对应类型流水，应答看今日新增应答条数
            try
            {
                var rule = await _ruleRepository.GetEnabledByTypeAsync(PointTransaction.TypeDailyCombo, cancellationToken);
                if (rule == null)
                    return 0;

                var bizId = $"daily_combo:{userId:N}:{BusinessClock.DateKey}";
                if (await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                    return 0;

                var todayUtc = BusinessClock.TodayUtc;
                var doneTypes = await _transactionRepository.GetGrantTypesAsync(userId, todayUtc, cancellationToken);
                if (!doneTypes.Contains(PointTransaction.TypeDailyCheckin))
                    return 0;
                if (!doneTypes.Contains(PointTransaction.TypeCorrectionAdopted))
                    return 0;
                var respondedToday = await _sourcingResponses.CountRespondedTodayByUserAsync(userId, cancellationToken);
                if (respondedToday <= 0)
                    return 0;

                // 三项全完成：走标准发放（日上限=1 天然防多次），失败吞掉不抛
                var amount = await GrantAsync(userId, PointTransaction.TypeDailyCombo, bizId,
                    "每日任务板三件套：签到 + 纠错 + 应答", null, cancellationToken);
                _logger.LogInformation("三件套发放: User={UserId}, Type={Type}, Amount={Amount}", userId, PointTransaction.TypeDailyCombo, amount);
                return amount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "三件套判定失败（按未发放）: User={UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// 解析阶梯 JSON 取第 streak 档分值；解析失败/无阶梯返回 null（调用方回退基础分值）
        /// </summary>
        private static int? PickLadderAmount(string? ladderJson, int streak)
        {
            if (string.IsNullOrWhiteSpace(ladderJson))
                return null;
            try
            {
                var ladder = JsonSerializer.Deserialize<List<int>>(ladderJson);
                if (ladder == null || ladder.Count == 0)
                    return null;
                return ladder[Math.Min(streak, ladder.Count) - 1];
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 暴击判定（v2.8.0 G1）：规则配置了暴击概率才生效——先掷传说（×5）再掷双倍（×2）；
        /// 两者皆未中返回原额。RNG 全程服务端，客户端只展示动画不参与判定（防伪造）
        /// </summary>
        /// <param name="rule">发放规则（携带暴击概率配置）</param>
        /// <param name="amount">基础金额（已含商家 buff）</param>
        /// <returns>暴击后的金额（未中=原额）</returns>
        private static int RollCrit(PointGrantRule rule, int amount)
        {
            if (rule.DoubleChance <= 0 && rule.LegendChance <= 0)
                return amount;
            var roll = Random.Shared.Next(1, 101);
            if (rule.LegendChance > 0 && roll <= rule.LegendChance)
                return amount * 5;
            if (rule.DoubleChance > 0 && roll <= rule.DoubleChance + rule.LegendChance)
                return amount * 2;
            return amount;
        }
    }
}
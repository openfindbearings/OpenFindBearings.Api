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
        // 改动说明（v2.4.0 工会经济）：成员合格赚分后向所属商户金库微量上供（trickle），
        // 白名单与上限全部在金库服务内部裁决；发放成功后同上下文调用（同请求同库，无需新事务）
        private readonly IMerchantPointsService _merchantPoints;
        // 改动说明（v2.5.0 工会经济）：成员被动加成——签到/登录/纠错按"最佳工会"等级加成，
        // 在日上限截顶之前套用（加成结果仍受 DailyLimit 约束，防叠出无顶收益）
        private readonly IMerchantGradeService _guilds;

        /// <summary>
        /// 构造：账户/流水/规则/台账仓储 + 工作单元（独立提交用）+ 上下文（失败清理用）+ 金库服务（trickle 挂钩）+ 等级服务（buff）
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
            IMerchantGradeService guilds)
        {
            _logger = logger;
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _ruleRepository = ruleRepository;
            _claimRepository = claimRepository;
            _unitOfWork = unitOfWork;
            _context = context;
            _merchantPoints = merchantPoints;
            _guilds = guilds;
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

                // 改动说明（v2.5.0 工会经济）：工会 buff 在截顶前套用——
                // 登录 Lv2+1、纠错 Lv2×1.1/Lv3×1.2/Lv4×1.25；其余场景原额返回零查询
                amount = await ApplyGuildBuffAsync(userId, grantType, amount, cancellationToken);

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
            string? remark = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // 先向台账原子占坑：键=手机号/信用代码等跨账号不变量。
                // 占到坑才发奖；流水 bizId 同步写同键做第二道幂等（同日双击/重放场景）
                var claimed = await _claimRepository.TryClaimAsync(claimKey, grantType, userId, cancellationToken);
                if (!claimed)
                    return 0; // 该号/照历史已领过：注销重注册/删店重入驻循环免疫
                return await GrantAsync(userId, grantType, claimKey, remark, null, cancellationToken);
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

            // 阶梯：按连续天数取档（[2,3,4,5,5] 第 6 天起恒取末档 5）；无阶梯配置回退基础分值
            var streak = account.MarkCheckedIn(today);
            var amount = PickLadderAmount(rule.LadderJson, streak) ?? rule.Amount;
            // 改动说明（v2.5.0 工会经济）：签到加成为工会 Lv1~+1/Lv2+1/Lv3+2/Lv4+3（阶梯后叠加）
            try
            {
                var guild = await _guilds.GetBestForUserAsync(userId, cancellationToken);
                amount += GuildBuffs.CheckinBonus(guild?.Grade ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "签到工会 buff 查询失败（按无加成）: User={UserId}", userId);
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

            return new CheckinResult(amount, streak, false);
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

            // 改动说明（v2.4.0 工会经济）：入账成功后尝试向成员所属商户金库上供——
            // 白名单（审核/交易类）在金库服务内部校验，登录/签到/注册等被动项自动跳过；
            // 同上下文提交但金库自行吞失败，绝不反噬个人赚分
            await _merchantPoints.TrickleForEarningAsync(userId, grantType, amount, bizId, cancellationToken);
        }

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
        /// 工会 buff 套用（v2.5.0）：仅白名单场景触发最佳工会查询（一次读，低频可接受）；
        /// 查询异常时按无 buff 处理——加成是锦上添花，绝不阻断发放
        /// </summary>
        private async Task<int> ApplyGuildBuffAsync(Guid userId, string grantType, int amount, CancellationToken ct)
        {
            if (grantType != PointTransaction.TypeDailyLogin && grantType != PointTransaction.TypeCorrectionAdopted)
                return amount;
            try
            {
                var guild = await _guilds.GetBestForUserAsync(userId, ct);
                if (guild == null)
                    return amount;
                return grantType switch
                {
                    PointTransaction.TypeDailyLogin => amount + GuildBuffs.LoginBonus(guild.Grade),
                    PointTransaction.TypeCorrectionAdopted => GuildBuffs.ApplyCorrectionBonus(amount, guild.Grade),
                    _ => amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "工会 buff 查询失败（按无加成发放）: User={UserId}", userId);
                return amount;
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
    }
}

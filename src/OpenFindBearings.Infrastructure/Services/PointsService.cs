using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

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
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// 构造：账户/流水/规则仓储 + 工作单元（独立提交用）
        /// </summary>
        public PointsService(
            ILogger<PointsService> logger,
            IPointAccountRepository accountRepository,
            IPointTransactionRepository transactionRepository,
            IPointGrantRuleRepository ruleRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _ruleRepository = ruleRepository;
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc/>
        public async Task<int> GrantAsync(Guid userId, string grantType, string? bizId = null,
            string? remark = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var rule = await _ruleRepository.GetEnabledByTypeAsync(grantType, cancellationToken);
                if (rule == null)
                    return 0; // 规则停用/不存在：静默跳过（运营关口的正常态）

                if (bizId != null && await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                    return 0; // 幂等：同动作重复发放

                var amount = rule.Amount;
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
            await _transactionRepository.AddAsync(new PointTransaction(
                userId, PointTransaction.DirectionDebit, sceneType, amount, account.Balance, bizId, remark), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("积分扣减: UserId={UserId}, Scene={Scene}, Amount={Amount}", userId, sceneType, amount);
            return amount;
        }

        /// <inheritdoc/>
        public async Task<CheckinResult> CheckinAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var bizId = $"checkin:{userId:N}:{today:yyyyMMdd}";
            if (await _transactionRepository.ExistsBizIdAsync(bizId, cancellationToken))
                return new CheckinResult(0, 0, true);

            var rule = await _ruleRepository.GetEnabledByTypeAsync(PointTransaction.TypeDailyCheckin, cancellationToken);
            if (rule == null)
                return new CheckinResult(0, 0, true); // 签到口被运营停用

            var account = await _accountRepository.GetByUserIdAsync(userId, cancellationToken)
                ?? new PointAccount(userId);

            // 阶梯：按连续天数取档（[2,3,4,5,5] 第 6 天起恒取末档 5）；无阶梯配置回退基础分值
            var streak = account.MarkCheckedIn(today);
            var amount = PickLadderAmount(rule.LadderJson, streak) ?? rule.Amount;

            account.Credit(amount);
            if (account.Id == Guid.Empty)
                await _accountRepository.AddAsync(account, cancellationToken);
            else
                await _accountRepository.UpdateAsync(account, cancellationToken);

            await _transactionRepository.AddAsync(new PointTransaction(
                userId, PointTransaction.DirectionCredit, PointTransaction.TypeDailyCheckin,
                amount, account.Balance, bizId, $"连续第 {streak} 天"), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CheckinResult(amount, streak, false);
        }

        /// <summary>
        /// 开户或取已有账户 → 入账 → 写流水 → 独立提交（GrantCore 失败由调用方吞）
        /// </summary>
        private async Task GrantCoreAsync(Guid userId, string grantType, int amount, string? bizId,
            string? remark, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
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
            await _transactionRepository.AddAsync(new PointTransaction(
                userId, PointTransaction.DirectionCredit, grantType, amount, account.Balance, bizId, remark), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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

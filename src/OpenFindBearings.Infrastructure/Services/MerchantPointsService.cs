using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家金库服务实现（v2.4.0 商家经济）。
    /// 所有参数走 SystemConfig（Business.* 键，Admin 可改实时生效），与个人积分规则表同理但更轻——
    /// 金库只有四个数，不建第二套规则表；日/月顶统计走流水索引单条 SUM
    /// </summary>
    public class MerchantPointsService : IMerchantPointsService
    {
        /// <summary>trickle 白名单：只有"人工审核/真实交易"类赚分上供——
        /// 登录/签到/注册是被动或无验证来源，进白名单即刷分水泵（定案于设计 v2.1.0）</summary>
        private static readonly HashSet<string> TrickleWhitelist = new(StringComparer.Ordinal)
        {
            PointTransaction.TypeCorrectionAdopted,
            PointTransaction.TypeMerchantApproved,
            PointTransaction.TypeMerchantProfileComplete,
            PointTransaction.TypeMerchantFirstProduct
        };

        private readonly IMerchantPointAccountRepository _accounts;
        private readonly IMerchantPointTransactionRepository _transactions;
        private readonly IMerchantMemberRepository _members;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MerchantPointsService> _logger;

        public MerchantPointsService(
            IMerchantPointAccountRepository accounts,
            IMerchantPointTransactionRepository transactions,
            IMerchantMemberRepository members,
            ISystemConfigRepository configs,
            IUnitOfWork unitOfWork,
            ILogger<MerchantPointsService> logger)
        {
            _accounts = accounts;
            _transactions = transactions;
            _members = members;
            _configs = configs;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<MerchantPointAccount?> GetAccountAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => _accounts.GetByMerchantIdAsync(merchantId, cancellationToken);

        /// <inheritdoc/>
        public Task<(List<MerchantPointTransaction> Items, int Total)> GetTransactionsAsync(
            Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
            => _transactions.GetByMerchantPagedAsync(merchantId, page, pageSize, cancellationToken);

        /// <inheritdoc/>
        public async Task TrickleForEarningAsync(Guid userId, string grantType, int memberAmount, string? sourceBizId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TrickleWhitelist.Contains(grantType) || memberAmount <= 0 || string.IsNullOrEmpty(sourceBizId))
                    return; // 非白名单/无来源幂等键：不上供（宁可漏发不可错发）

                var percent = await GetConfigAsync("Business.TricklePercent", 10, cancellationToken);
                var total = memberAmount * percent / 100;
                if (total <= 0) return;

                // 平分全部在职归属（用户定案 §6980：一家全给，多家平分——总量守恒，多归属不放大）
                var memberships = (await _members.GetActiveByUserIdAsync(userId, cancellationToken))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();
                if (memberships.Count == 0) return; // 散人无商家

                var n = memberships.Count;
                var each = total / n;
                var remainder = total % n;

                var dayCap = await GetConfigAsync("Business.TrickleDailyCapPerMerchant", 50, cancellationToken);
                var monthCap = await GetConfigAsync("Business.TrickleMonthlyCapPerMerchant", 1000, cancellationToken);

                foreach (var membership in memberships)
                {
                    // 余数给最早加入的一家（M2 有等级后改"最高等级家"，排序已是加入时间序）
                    var share = each + (ReferenceEquals(membership, memberships[0]) ? remainder : 0);
                    if (share <= 0) continue;
                    await CreditCappedAsync(membership.MerchantId, share,
                        MerchantPointTransaction.TypeMemberTrickle,
                        $"trickle:{sourceBizId}:{membership.MerchantId:N}",
                        $"{grantType} 上供", dayCap, monthCap, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // 金库吞失败：上供是附属账本，绝不反噬成员赚分/业务主流程（同 PointsService 纪律）
                _logger.LogWarning(ex, "商家金库 trickle 失败: User={UserId}, Type={Type}", userId, grantType);
            }
        }

        /// <inheritdoc/>
        public async Task<int> SettleOrderAsync(MallOrder order, Guid merchantId, CancellationToken cancellationToken = default)
        {
            var bizId = $"settle:{order.Id:N}";
            if (await _transactions.ExistsBizIdAsync(bizId, cancellationToken))
                return 0; // 幂等：同一订单只结一次

            var monthCap = await GetConfigAsync("Business.GiftSettleMonthlyCap", 2000, cancellationToken);
            var monthUsed = monthCap > 0
                ? await _transactions.SumCreditSinceAsync(merchantId, MerchantPointTransaction.TypeGiftSettlement, BusinessMonthStartUtc(), cancellationToken)
                : 0;
            var settle = monthCap > 0 ? Math.Min(order.PointsSpent, Math.Max(0, monthCap - monthUsed)) : order.PointsSpent;
            if (settle <= 0)
            {
                _logger.LogWarning("金库月顶已满，本单不结算: Merchant={MerchantId}, Order={OrderId}", merchantId, order.Id);
                return 0;
            }

            // 懒开户：新建走 Add、已有走 Update（与 PointsService 同款显式仓储路径，不依赖导航自动发现）
            var account = await _accounts.GetByMerchantIdAsync(merchantId, cancellationToken);
            if (account == null)
            {
                account = new MerchantPointAccount(merchantId);
                await _accounts.AddAsync(account, cancellationToken);
            }
            else
            {
                await _accounts.UpdateAsync(account, cancellationToken);
            }

            account.Credit(settle);
            await _transactions.AddAsync(new MerchantPointTransaction(
                merchantId, MerchantPointTransaction.DirectionCredit, MerchantPointTransaction.TypeGiftSettlement,
                settle, account.Balance, bizId, $"礼品订单 {order.ItemName} 确认收货结算"), cancellationToken);

            return settle;
        }

        /// <inheritdoc/>
        public async Task<bool> SpendAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default)
        {
            if (await _transactions.ExistsBizIdAsync(bizId, cancellationToken))
                return false; // 幂等命中：同笔消费不重复记账（调用方据此判重复提交）

            var account = await _accounts.GetByMerchantIdAsync(merchantId, cancellationToken)
                ?? throw new InvalidOperationException("商家金库未开户");

            account.Debit(amount); // 余额不足抛出，调用方转 400
            await _accounts.UpdateAsync(account, cancellationToken);
            await _transactions.AddAsync(new MerchantPointTransaction(
                merchantId, MerchantPointTransaction.DirectionDebit, MerchantPointTransaction.TypeTreasurySpend,
                amount, account.Balance, bizId, remark), cancellationToken);
            // 不在此提交——由调用方（商城兑换）把"金库支出+订单"同批 SaveChanges 保原子
            return true;
        }

        /// <inheritdoc/>
        public async Task GrantGradeUpBonusAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (amount <= 0)
                    return;
                if (await _transactions.ExistsBizIdAsync(bizId, cancellationToken))
                    return; // 幂等：每商户每档终身一次，复升同档不重发

                // 懒开户（与结算/任务奖励同款显式仓储路径），入账+流水独立提交
                var account = await _accounts.GetByMerchantIdAsync(merchantId, cancellationToken);
                if (account == null)
                {
                    account = new MerchantPointAccount(merchantId);
                    await _accounts.AddAsync(account, cancellationToken);
                }
                else
                {
                    await _accounts.UpdateAsync(account, cancellationToken);
                }
                account.Credit(amount);
                await _transactions.AddAsync(new MerchantPointTransaction(
                    merchantId, MerchantPointTransaction.DirectionCredit, MerchantPointTransaction.TypeGradeUpBonus,
                    amount, account.Balance, bizId, remark), cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("商家升档礼入账: Merchant={MerchantId}, Amount={Amount}, BizId={BizId}",
                    merchantId, amount, bizId);
            }
            catch (Exception ex)
            {
                // 吞失败不反噬等级重算与业务主流程（同 trickle 纪律）
                _logger.LogWarning(ex, "商家升档礼入账失败: Merchant={MerchantId}, BizId={BizId}", merchantId, bizId);
            }
        }

        /// <inheritdoc/>
        public async Task BurnOnReleaseAsync(Guid merchantId, string bizId, CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByMerchantIdAsync(merchantId, cancellationToken);
            if (account == null || account.Balance <= 0)
                return; // 无金库或已空：无需燃烧

            // 商家解散仓库回收：余额全额出账，与释放/删除动作同事务提交（调用方 SaveChanges）
            var amount = account.Balance;
            account.Debit(amount);
            await _accounts.UpdateAsync(account, cancellationToken);
            await _transactions.AddAsync(new MerchantPointTransaction(
                merchantId, MerchantPointTransaction.DirectionDebit, MerchantPointTransaction.TypeBurn,
                amount, account.Balance, bizId, "关店/解除归属：金库余额燃烧"), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RewardTreasuryAsync(Guid merchantId, int amount, string bizId, string? remark,
            CancellationToken cancellationToken = default)
        {
            // v2.6.0 集体任务奖励入账：幂等命中直接跳过；不在此提交（调用方与完成台账同批保原子）
            if (await _transactions.ExistsBizIdAsync(bizId, cancellationToken))
                return;

            var account = await _accounts.GetByMerchantIdAsync(merchantId, cancellationToken);
            if (account == null)
            {
                account = new MerchantPointAccount(merchantId);
                await _accounts.AddAsync(account, cancellationToken);
            }
            else
            {
                await _accounts.UpdateAsync(account, cancellationToken);
            }
            account.Credit(amount);
            await _transactions.AddAsync(new MerchantPointTransaction(
                merchantId, MerchantPointTransaction.DirectionCredit, MerchantPointTransaction.TypeMerchantTaskReward,
                amount, account.Balance, bizId, remark), cancellationToken);
        }

        /// <summary>
        /// 受日上限/月上限约束的单家入账（超限部分直接放弃——顶是防刷闸不是欠账）；
        /// 日/月已用量内部按 BusinessClock 口径统计；不在此 SaveChanges，由调用方统一提交
        /// </summary>
        private async Task CreditCappedAsync(Guid merchantId, int want, string grantType, string bizId,
            string remark, int dayCap, int monthCap, CancellationToken ct)
        {
            if (await _transactions.ExistsBizIdAsync(bizId, ct))
                return; // 幂等：同笔上供重复计入

            var amount = want;
            if (dayCap > 0)
                amount = Math.Min(amount, dayCap - await _transactions.SumCreditSinceAsync(
                    merchantId, grantType, BusinessClock.TodayUtc, ct));
            if (monthCap > 0)
                amount = Math.Min(amount, monthCap - await _transactions.SumCreditSinceAsync(
                    merchantId, grantType, BusinessMonthStartUtc(), ct));
            if (amount <= 0) return; // 顶满：本日/本月不再收

            var account = await _accounts.GetByMerchantIdAsync(merchantId, ct);
            if (account == null)
            {
                account = new MerchantPointAccount(merchantId);
                await _accounts.AddAsync(account, ct);
            }
            else
            {
                await _accounts.UpdateAsync(account, ct);
            }
            account.Credit(amount);
            await _transactions.AddAsync(new MerchantPointTransaction(
                merchantId, MerchantPointTransaction.DirectionCredit, grantType,
                amount, account.Balance, bizId, remark), ct);
        }

        /// <summary>业务月首（UTC）：BusinessClock 口径的自然月 1 日 0 点换算回 UTC 存储值</summary>
        private static DateTime BusinessMonthStartUtc()
        {
            var bNow = BusinessClock.Now;
            var bMonthStart = new DateTime(bNow.Year, bNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return DateTime.SpecifyKind(bMonthStart - BusinessClock.Offset, DateTimeKind.Utc);
        }

        /// <summary>读 Business.* 整型配置（解析失败/缺失回退默认，负数视为无效）</summary>
        private async Task<int> GetConfigAsync(string key, int fallback, CancellationToken ct)
        {
            var parsed = await _configs.GetValueAsync<int?>(key, null);
            return parsed.HasValue && parsed.Value >= 0 ? parsed.Value : fallback;
        }
    }
}

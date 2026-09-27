using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 工会等级服务实现（v2.5.0 工会经济）：等级重算规则 + 成员最佳工会解析。
    /// 阈值走 SystemConfig（Business.Guild* 键，Admin 可调实时生效）；
    /// 重算吞失败（等级是附属计算值），解析路径纯读不写
    /// </summary>
    public class MerchantGradeService : IMerchantGradeService
    {
        private readonly IMerchantRepository _merchants;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantBearingRepository _bearings;
        private readonly IMerchantPointAccountRepository _treasury;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MerchantGradeService> _logger;

        public MerchantGradeService(
            IMerchantRepository merchants,
            IMerchantMemberRepository members,
            IMerchantBearingRepository bearings,
            IMerchantPointAccountRepository treasury,
            ISystemConfigRepository configs,
            IUnitOfWork unitOfWork,
            ILogger<MerchantGradeService> logger)
        {
            _merchants = merchants;
            _members = members;
            _bearings = bearings;
            _treasury = treasury;
            _configs = configs;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task RecomputeAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 改动说明：先把调用方同请求的待写实体刷库（如刚改的 IsOnSale），
                // 否则本方法内的在售计数读到旧值；等级独立提交（失败吞掉不影响主事务）
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var merchant = await _merchants.GetByIdAsync(merchantId, cancellationToken);
                if (merchant == null || merchant.Status != MerchantStatus.Active)
                    return; // 非在营商户不定级（释放路径已由域方法落 Standard）

                var lv3Min = await GetConfigAsync("Business.GuildPremiumOnSaleMin", 5, cancellationToken);
                var lv4Min = await GetConfigAsync("Business.GuildGoldOnSaleMin", 10, cancellationToken);
                var lv4Treasury = await GetConfigAsync("Business.GuildGoldTreasuryMin", 500, cancellationToken);
                var account = await _treasury.GetByMerchantIdAsync(merchantId, cancellationToken);
                var treasuryEarned = account?.TotalEarned ?? 0;
                // 定级口径用实时在售明细数而非 Merchant.ProductCount 冗余列——
                // 该列仅在部分聚合路径刷新，商家自助上/下架不经过它，规则输入必须取实
                var onSaleCount = (await _bearings.GetOnSaleByMerchantAsync(merchantId, cancellationToken)).Count();

                var target = ComputeGrade(merchant.IsVerified, onSaleCount, treasuryEarned, lv3Min, lv4Min, lv4Treasury);
                if (target == merchant.Grade || target == MerchantGrade.Unknown)
                    return; // 无变化不落库不刷时间戳
                merchant.UpdateGrade(target);
                await _merchants.UpdateAsync(merchant, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "工会等级重算失败: Merchant={MerchantId}", merchantId);
            }
        }

        /// <inheritdoc/>
        public async Task<MemberGuildInfo?> GetBestForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var memberships = await _members.GetActiveByUserIdAsync(userId, cancellationToken);
            if (memberships.Count == 0)
                return null; // 散人无工会

            MemberGuildInfo? best = null;
            foreach (var membership in memberships.OrderBy(m => m.CreatedAt))
            {
                var merchant = await _merchants.GetByIdAsync(membership.MerchantId, cancellationToken);
                if (merchant == null || merchant.Status != MerchantStatus.Active)
                    continue;
                var info = new MemberGuildInfo(merchant.Id, merchant.Name, (int)merchant.Grade);
                if (best == null || GuildBuffs.Rank(info.Grade) > GuildBuffs.Rank(best.Grade))
                    best = info; // 平级保持先加入（OrderBy CreatedAt）——确定性口径
            }
            return best;
        }

        /// <summary>等级规则纯函数（数值语义见 MerchantGrade 枚举注释）</summary>
        private static MerchantGrade ComputeGrade(bool verified, int onSaleCount, int treasuryEarned,
            int lv3Min, int lv4Min, int lv4TreasuryMin)
        {
            if (!verified)
                return MerchantGrade.Standard;
            if (onSaleCount >= lv4Min && treasuryEarned >= lv4TreasuryMin)
                return MerchantGrade.Gold;
            if (onSaleCount >= lv3Min)
                return MerchantGrade.Premium;
            return MerchantGrade.Verified;
        }

        /// <summary>读 Business.* 整型配置（缺失/非法回退默认）</summary>
        private async Task<int> GetConfigAsync(string key, int fallback, CancellationToken ct)
        {
            var parsed = await _configs.GetValueAsync<int?>(key, null);
            return parsed.HasValue && parsed.Value >= 0 ? parsed.Value : fallback;
        }
    }
}

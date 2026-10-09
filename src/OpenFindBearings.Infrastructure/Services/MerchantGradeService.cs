using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家等级服务实现（v2.5.0 商家经济）：等级重算规则 + 成员最佳商家解析。
    /// 阈值走 SystemConfig（Business.Merchant* 键，Admin 可调实时生效）；
    /// 重算吞失败（等级是附属计算值），解析路径纯读不写。
    /// 改动说明（v2.12.0 等级玩法）：升档发一次性商家金升档礼（每商户每档终身一次，
    /// 复升不重发防上下架刷金库）；降档先进保级缓冲（Business.MerchantGradeGraceDays 天，
    /// 0=立即降），期内恢复达标自动清钟——buff 影响成员收益，降档要有挽回窗口
    /// </summary>
    public class MerchantGradeService : IMerchantGradeService
    {
        private readonly IMerchantRepository _merchants;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantBearingRepository _bearings;
        private readonly IMerchantPointAccountRepository _treasury;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        // 改动说明（v2.12.0 等级玩法）：升档礼入账走金库服务（吞失败纪律在金库侧）
        private readonly IMerchantPointsService _merchantPoints;
        private readonly ILogger<MerchantGradeService> _logger;

        public MerchantGradeService(
            IMerchantRepository merchants,
            IMerchantMemberRepository members,
            IMerchantBearingRepository bearings,
            IMerchantPointAccountRepository treasury,
            ISystemConfigRepository configs,
            IUnitOfWork unitOfWork,
            IMerchantPointsService merchantPoints,
            ILogger<MerchantGradeService> logger)
        {
            _merchants = merchants;
            _members = members;
            _bearings = bearings;
            _treasury = treasury;
            _configs = configs;
            _unitOfWork = unitOfWork;
            _merchantPoints = merchantPoints;
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

                var lv3Min = await GetConfigAsync("Business.MerchantPremiumOnSaleMin", 5, cancellationToken);
                var lv4Min = await GetConfigAsync("Business.MerchantGoldOnSaleMin", 10, cancellationToken);
                var lv4Treasury = await GetConfigAsync("Business.MerchantGoldTreasuryMin", 500, cancellationToken);
                var account = await _treasury.GetByMerchantIdAsync(merchantId, cancellationToken);
                var treasuryEarned = account?.TotalEarned ?? 0;
                // 定级口径用实时在售明细数而非 Merchant.ProductCount 冗余列——
                // 该列仅在部分聚合路径刷新，商家自助上/下架不经过它，规则输入必须取实
                var onSaleCount = (await _bearings.GetOnSaleByMerchantAsync(merchantId, cancellationToken)).Count();

                var target = ComputeGrade(merchant.IsVerified, onSaleCount, treasuryEarned, lv3Min, lv4Min, lv4Treasury);
                if (target == MerchantGrade.Unknown)
                    return;

                var oldRank = MerchantBuffs.Rank((int)merchant.Grade);
                var newRank = MerchantBuffs.Rank((int)target);

                if (newRank == oldRank)
                {
                    // 保级期内恢复达标：计算档=持有档，撤掉挂起的降档钟（下次再掉档重新起钟）
                    if (merchant.GradeGraceUntil != null)
                    {
                        merchant.SetGradeGraceUntil(null);
                        await _merchants.UpdateAsync(merchant, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    return; // 无变化不落库不刷时间戳
                }

                if (newRank > oldRank)
                {
                    // 升档：即时生效（UpdateGrade 顺带清保级钟），跨过的每一档各发终身一次升档礼
                    merchant.UpdateGrade(target);
                    await _merchants.UpdateAsync(merchant, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    for (var rank = oldRank + 1; rank <= newRank; rank++)
                    {
                        var bonus = await GetGradeUpBonusAsync(rank, cancellationToken);
                        if (bonus > 0)
                            await _merchantPoints.GrantGradeUpBonusAsync(merchantId, bonus,
                                $"gradeup:{merchantId:N}:{rank}", $"商家等级升档礼：Lv{rank}", cancellationToken);
                    }
                    return;
                }

                // 降档：先过保级缓冲闸（v2.12.0）——挂钟期内维持原档与成员 buff
                var graceDays = await GetConfigAsync("Business.MerchantGradeGraceDays", 15, cancellationToken);
                if (graceDays <= 0)
                {
                    merchant.UpdateGrade(target); // 0=关闭缓冲，立即落档
                }
                else if (merchant.GradeGraceUntil == null)
                {
                    merchant.SetGradeGraceUntil(DateTime.UtcNow.AddDays(graceDays)); // 首次发现不达标：挂钟保级
                }
                else if (DateTime.UtcNow >= merchant.GradeGraceUntil)
                {
                    merchant.UpdateGrade(target); // 保级到期仍不达标：降档落实
                }
                else
                {
                    return; // 保级期内：维持原档不动库
                }
                await _merchants.UpdateAsync(merchant, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "商家等级重算失败: Merchant={MerchantId}", merchantId);
            }
        }

        /// <summary>
        /// 商家等级升档礼金额（v2.12.0 等级玩法）：按等级序数读 Business.MerchantGradeBonusLv2/3/4
        /// 配置（Admin 可调），缺失/非法回退默认 50/150/300；Lv1 无档礼返回 0
        /// </summary>
        private async Task<int> GetGradeUpBonusAsync(int rank, CancellationToken ct) => rank switch
        {
            2 => await GetConfigAsync("Business.MerchantGradeBonusLv2", 50, ct),
            3 => await GetConfigAsync("Business.MerchantGradeBonusLv3", 150, ct),
            4 => await GetConfigAsync("Business.MerchantGradeBonusLv4", 300, ct),
            _ => 0
        };

        /// <inheritdoc/>
        public async Task<MemberMerchantInfo?> GetBestForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var memberships = await _members.GetActiveByUserIdAsync(userId, cancellationToken);
            if (memberships.Count == 0)
                return null; // 散人无商家

            MemberMerchantInfo? best = null;
            foreach (var membership in memberships.OrderBy(m => m.CreatedAt))
            {
                var merchant = await _merchants.GetByIdAsync(membership.MerchantId, cancellationToken);
                if (merchant == null || merchant.Status != MerchantStatus.Active)
                    continue;
                var info = new MemberMerchantInfo(merchant.Id, merchant.Name, (int)merchant.Grade);
                if (best == null || MerchantBuffs.Rank(info.Grade) > MerchantBuffs.Rank(best.Grade))
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

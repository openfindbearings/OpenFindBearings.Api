using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家牌级服务实现（v2.19.0 有效期制）：认证门槛 + 实时在售闸门 + 本有效期成交次数。
    /// 阈值走 SystemConfig（Business.Merchant* 键，Admin 可调实时生效）；重算吞失败（牌级是附属计算值），
    /// 解析路径纯读不写。
    /// 有效期规则：
    ///   1. 认证商户至少铜牌，起期 180 天（Business.MerchantGradeWindowDays）；
    ///   2. 期内成交达下一档阈值且在售达标即升档，重发 180 天、计数清零；
    ///   3. 到期结算：本期成交达当前档要求则续 180 天（起点取原到期日），否则降到本期成交能支撑的最高档；
    ///   4. 在售连续不足超过保级期（Business.MerchantGradeGraceDays）直接降普通，不等期末；
    ///   5. 升档礼每档终身一次（复升不重发），降档不追回已发商家金。
    /// 金库累计不参与定级（金库是平台激励货币账户，非供货能力）。
    /// </summary>
    public class MerchantGradeService : IMerchantGradeService
    {
        private readonly IMerchantRepository _merchants;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantBearingRepository _bearings;
        // 成功撮合次数取自寻货应答仓储（计分回执）
        private readonly ISourcingResponseRepository _responses;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        // 升档礼入账走金库服务（吞失败纪律在金库侧）
        private readonly IMerchantPointsService _merchantPoints;
        private readonly ILogger<MerchantGradeService> _logger;

        public MerchantGradeService(
            IMerchantRepository merchants,
            IMerchantMemberRepository members,
            IMerchantBearingRepository bearings,
            ISourcingResponseRepository responses,
            ISystemConfigRepository configs,
            IUnitOfWork unitOfWork,
            IMerchantPointsService merchantPoints,
            ILogger<MerchantGradeService> logger)
        {
            _merchants = merchants;
            _members = members;
            _bearings = bearings;
            _responses = responses;
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
                // 先把调用方同请求的待写实体刷库（如刚改的 IsOnSale），否则本方法内的在售计数读到旧值；
                // 等级独立提交（失败吞掉不影响主事务）
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var merchant = await _merchants.GetByIdAsync(merchantId, cancellationToken);
                if (merchant == null || merchant.Status != MerchantStatus.Active)
                    return; // 非在营商户不定级（释放路径已由域方法落普通）

                var onSaleMin = await GetConfigAsync(MerchantGradeSettings.GradeOnSaleMinKey, MerchantGradeSettings.GradeOnSaleMinDefault, cancellationToken);
                var silverDealMin = await GetConfigAsync(MerchantGradeSettings.SilverDealMinKey, MerchantGradeSettings.SilverDealMinDefault, cancellationToken);
                var goldDealMin = await GetConfigAsync(MerchantGradeSettings.GoldDealMinKey, MerchantGradeSettings.GoldDealMinDefault, cancellationToken);
                var periodDays = await GetConfigAsync(MerchantGradeSettings.WindowDaysKey, MerchantGradeSettings.WindowDaysDefault, cancellationToken);
                var graceDays = await GetConfigAsync(MerchantGradeSettings.GraceDaysKey, MerchantGradeSettings.GraceDaysDefault, cancellationToken);

                var now = DateTime.UtcNow;
                // 改动说明（v2.19.0 有效期制）：记录重算前档位——认证后"补铜牌"发生在升档分支之前，
                // 若不单独比对，新认证商户的铜牌升档礼会被漏发（首期成交为 0，走不到升档分支）
                var previousRank = MerchantBuffs.Rank((int)merchant.Grade);
                // 定级口径用实时"公域可见在售"明细数（出售中 + 补货中），不读 ProductCount 冗余列
                var onSaleCount = (await _bearings.GetMatchableByMerchantAsync(merchantId, cancellationToken)).Count();
                // 本有效期成交：起期为空说明尚未起期，按本次重算时点起算（历史成交不计入新周期）
                var periodStart = merchant.GradePeriodStartAt ?? now;
                var dealCount = await _responses.GetCountedDealCountAsync(merchantId, periodStart, cancellationToken);

                // 未认证：一律普通，清理残留有效期与保级钟
                if (!merchant.IsVerified)
                {
                    if (merchant.Grade != MerchantGrade.Ordinary || merchant.GradePeriodEndAt != null || merchant.GradeGraceUntil != null)
                    {
                        merchant.UpdateGrade(MerchantGrade.Ordinary);
                        await _merchants.UpdateAsync(merchant, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    return;
                }

                // 在售闸门（实时，不按有效期豁免）：连续不足超过保级期直接降普通
                if (onSaleCount < onSaleMin)
                {
                    if (merchant.Grade == MerchantGrade.Ordinary)
                    {
                        // 已是普通：无牌可保，清掉可能残留的保级钟即可
                        if (merchant.GradeGraceUntil != null)
                        {
                            merchant.SetGradeGraceUntil(null);
                            await _merchants.UpdateAsync(merchant, cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }
                        return;
                    }

                    if (graceDays <= 0)
                    {
                        // 关闭缓冲：一旦发现不足立即落普通
                        merchant.UpdateGrade(MerchantGrade.Ordinary);
                        await _merchants.UpdateAsync(merchant, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        return;
                    }

                    if (merchant.GradeGraceUntil == null)
                    {
                        // 首次发现不足：挂保级钟，期内维持原档与成员 buff
                        merchant.SetGradeGraceUntil(now.AddDays(graceDays));
                        await _merchants.UpdateAsync(merchant, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        return;
                    }

                    if (now < merchant.GradeGraceUntil)
                        return; // 保级期内：维持原档不动库

                    // 保级到期仍不足：落普通
                    merchant.UpdateGrade(MerchantGrade.Ordinary);
                    await _merchants.UpdateAsync(merchant, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return;
                }

                // 在售已恢复：清除保级钟（可能同时需要继续判定升/降档，最后统一保存）
                var dirty = false;
                if (merchant.GradeGraceUntil != null)
                {
                    merchant.SetGradeGraceUntil(null);
                    dirty = true;
                }

                // 认证商户最低铜牌：历史数据/人工调整可能停在普通，先补档（UpdateGrade 会清有效期，随后重起）
                if (merchant.Grade == MerchantGrade.Ordinary)
                {
                    merchant.UpdateGrade(MerchantGrade.Bronze);
                    dirty = true;
                }

                var currentRank = MerchantBuffs.Rank((int)merchant.Grade);

                // 有效期缺失（刚认证或历史数据）：以当前时点起期，本周期成交从 0 起算
                if (merchant.GradePeriodStartAt == null || merchant.GradePeriodEndAt == null)
                {
                    merchant.SetGradePeriod(now, now.AddDays(periodDays));
                    dealCount = 0;
                    dirty = true;
                }

                var periodEnd = merchant.GradePeriodEndAt!.Value;
                // 本期成交能支撑的档位（在售已达标，因此至少铜牌）
                var capability = ComputeGrade(dealCount, silverDealMin, goldDealMin);
                var capabilityRank = MerchantBuffs.Rank((int)capability);

                // 补档升档（普通 → 铜牌）也要发一次升档礼：发放按 bizId 幂等，重复调用不会重复入账
                if (currentRank > previousRank)
                {
                    await _merchants.UpdateAsync(merchant, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    for (var rank = previousRank + 1; rank <= currentRank; rank++)
                    {
                        var bonus = await GetGradeUpBonusAsync(rank, cancellationToken);
                        if (bonus > 0)
                            await _merchantPoints.GrantGradeUpBonusAsync(merchantId, bonus,
                                $"gradeup:{merchantId:N}:{rank}", $"商家牌级升档礼：Lv{rank}", cancellationToken);
                    }
                }

                // 期内达下一档：立即升级并重发有效期（计数清零）
                if (capabilityRank > currentRank)
                {
                    merchant.UpdateGrade(capability);
                    merchant.SetGradePeriod(now, now.AddDays(periodDays));
                    await _merchants.UpdateAsync(merchant, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    for (var rank = 2; rank <= capabilityRank; rank++)
                    {
                        var bonus = await GetGradeUpBonusAsync(rank, cancellationToken);
                        if (bonus > 0)
                            await _merchantPoints.GrantGradeUpBonusAsync(merchantId, bonus,
                                $"gradeup:{merchantId:N}:{rank}", $"商家牌级升档礼：Lv{rank}", cancellationToken);
                    }
                    return;
                }

                if (capabilityRank == currentRank)
                {
                    // 到期续期：起点取原到期日，不留空档；未到期则无需动作。
                    // 改动说明：巡检或服务停机跨过多个周期时逐期前推，避免续出的周期仍在过去
                    // （否则每天续一次 180 天、要追很多天才回到当下）
                    if (now >= periodEnd)
                    {
                        var nextStart = periodEnd;
                        while (nextStart.AddDays(periodDays) <= now)
                            nextStart = nextStart.AddDays(periodDays);
                        merchant.SetGradePeriod(nextStart, nextStart.AddDays(periodDays));
                        dirty = true;
                    }
                }
                else if (now >= periodEnd)
                {
                    // 到期成交不足以保级：降到本期成交能支撑的最高档（0 单降铜牌），并重起有效期
                    merchant.UpdateGrade(capability);
                    merchant.SetGradePeriod(now, now.AddDays(periodDays));
                    dirty = true;
                }
                // 未到期且成交不足：期内不考核成交，等到期结算

                if (dirty)
                {
                    await _merchants.UpdateAsync(merchant, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
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
            2 => await GetConfigAsync(MerchantGradeSettings.BonusLv2Key, MerchantGradeSettings.BonusLv2Default, ct),
            3 => await GetConfigAsync(MerchantGradeSettings.BonusLv3Key, MerchantGradeSettings.BonusLv3Default, ct),
            4 => await GetConfigAsync(MerchantGradeSettings.BonusLv4Key, MerchantGradeSettings.BonusLv4Default, ct),
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

        /// <summary>
        /// 牌级规则纯函数（v2.19.0 有效期制）：在售与认证由调用方先行判定，
        /// 此处只按本有效期成交次数落到铜/银/金牌
        /// </summary>
        private static MerchantGrade ComputeGrade(int dealCount, int silverDealMin, int goldDealMin)
        {
            if (dealCount >= goldDealMin)
                return MerchantGrade.Gold;
            if (dealCount >= silverDealMin)
                return MerchantGrade.Silver;
            return MerchantGrade.Bronze;
        }

        /// <summary>读 Business.* 整型配置（缺失/非法回退默认）</summary>
        private async Task<int> GetConfigAsync(string key, int fallback, CancellationToken ct)
        {
            var parsed = await _configs.GetValueAsync<int?>(key, null);
            return parsed.HasValue && parsed.Value >= 0 ? parsed.Value : fallback;
        }
    }
}

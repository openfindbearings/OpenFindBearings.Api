using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 商家卡级服务实现（v2.5.0 商家经济，v2.18.0 卡级重构改口径）：卡级重算规则 + 成员最佳商家解析。
    /// 阈值走 SystemConfig（Business.Merchant* 键，Admin 可调实时生效）；重算吞失败（卡级是附属计算值），解析路径纯读不写。
    /// 改动说明（v2.12.0 等级玩法）：升档发一次性商家金升档礼（每商户每档终身一次，复升不重发防上下架刷金库）；
    /// 降档先进保级缓冲（Business.MerchantGradeGraceDays 天，0=立即降），期内恢复达标自动清钟。
    /// 改动说明（v2.18.0 卡级重构）：定级口径改为「认证门槛 + 公域可见在售件数 + 近窗口期成功撮合次数」——
    ///   金库累计退出判定（金库是平台激励货币账户、非供货能力，且只增不减与"可降级卡级"语义冲突），
    ///   成功撮合以"需求方回执谈成 + 计分"为准（滚动窗口，滑出即衰减，使降级真实可发生）。
    /// </summary>
    public class MerchantGradeService : IMerchantGradeService
    {
        private readonly IMerchantRepository _merchants;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantBearingRepository _bearings;
        // 改动说明（v2.18.0 卡级重构）：成功撮合次数取自寻货应答仓储（计分回执），替代原金库账户仓储输入
        private readonly ISourcingResponseRepository _responses;
        private readonly ISystemConfigRepository _configs;
        private readonly IUnitOfWork _unitOfWork;
        // 改动说明（v2.12.0 等级玩法）：升档礼入账走金库服务（吞失败纪律在金库侧）
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
                // 改动说明：先把调用方同请求的待写实体刷库（如刚改的 IsOnSale），
                // 否则本方法内的在售计数读到旧值；等级独立提交（失败吞掉不影响主事务）
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var merchant = await _merchants.GetByIdAsync(merchantId, cancellationToken);
                if (merchant == null || merchant.Status != MerchantStatus.Active)
                    return; // 非在营商户不定级（释放路径已由域方法落 Standard）

                // 改动说明（v2.13.2 配置收口）：阈值/默认值统一走 MerchantGradeSettings，与端点侧同源
                // 改动说明（v2.18.0 卡级重构）：合并在售前置为单键，新增窗口天数与两档撮合次数，删除金库键
                var onSaleMin = await GetConfigAsync(MerchantGradeSettings.GradeOnSaleMinKey, MerchantGradeSettings.GradeOnSaleMinDefault, cancellationToken);
                var silverDealMin = await GetConfigAsync(MerchantGradeSettings.SilverDealMinKey, MerchantGradeSettings.SilverDealMinDefault, cancellationToken);
                var goldDealMin = await GetConfigAsync(MerchantGradeSettings.GoldDealMinKey, MerchantGradeSettings.GoldDealMinDefault, cancellationToken);
                var windowDays = await GetConfigAsync(MerchantGradeSettings.WindowDaysKey, MerchantGradeSettings.WindowDaysDefault, cancellationToken);
                // 定级口径用实时"公域可见在售"明细数而非 Merchant.ProductCount 冗余列——
                // 该列仅在部分聚合路径刷新，商家自助上/下架不经过它，规则输入必须取实时。
                // 改动说明（v1.8.2 在售口径统一）：由"仅出售中"改为"出售中+补货中（公域可见）"，
                //   与寻货匹配/SourcingMatcher 的可匹配口径一致——此前同一商家可能"被寻货通知却不计入等级在售"
                var onSaleCount = (await _bearings.GetMatchableByMerchantAsync(merchantId, cancellationToken)).Count();
                // 改动说明（v2.18.0）：成功撮合取自计分回执（谈成），近 windowDays 滚动窗口——窗口滑出即衰减，卡级可真降
                var dealCount = await _responses.GetCountedDealCountAsync(merchantId, DateTime.UtcNow.AddDays(-windowDays), cancellationToken);

                var target = ComputeGrade(merchant.IsVerified, onSaleCount, dealCount, onSaleMin, silverDealMin, goldDealMin);
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
                var graceDays = await GetConfigAsync(MerchantGradeSettings.GraceDaysKey, MerchantGradeSettings.GraceDaysDefault, cancellationToken);
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
        /// 卡级规则纯函数（v2.18.0 卡级重构；数值语义见 MerchantGrade 枚举注释）：
        /// 未认证=注册商户；认证且在售达前置 + 近窗口期成功撮合达金/银阈值=金卡/银卡；其余=铜卡
        /// </summary>
        private static MerchantGrade ComputeGrade(bool verified, int onSaleCount, int dealCount,
            int onSaleMin, int silverDealMin, int goldDealMin)
        {
            if (!verified)
                return MerchantGrade.Registered;
            if (onSaleCount >= onSaleMin && dealCount >= goldDealMin)
                return MerchantGrade.Gold;
            if (onSaleCount >= onSaleMin && dealCount >= silverDealMin)
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

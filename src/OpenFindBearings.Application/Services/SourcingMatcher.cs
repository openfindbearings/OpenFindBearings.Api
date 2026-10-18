using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 寻货匹配器（v1.7.0 匹配口径统一）：集中"广播单应推送给哪些商户"的唯一口径，
    /// 供发布处理器（回传已匹配家数，供前端区分两条路径）与发布通知订阅者（逐户推送）共用，避免两处口径漂移。
    /// 口径：公域可见（审核通过 且 出售中/补货中）本轴承的关联商户，去重后仅保留已认证商家（可信数据）。
    /// </summary>
    public static class SourcingMatcher
    {
        /// <summary>
        /// 取本次寻货应推送的认证商家 ID 集合。
        /// 无 bearingId（如从发现页直接发布、自由文本型号）时按型号文本精确匹配轴承主数据兜底；
        /// 未命中主数据（长尾自由型号）或无人匹配时返回空集合。
        /// </summary>
        /// <param name="bearingRepository">轴承主数据仓储（型号文本兜底匹配用）</param>
        /// <param name="merchantBearingRepository">商家商品关联仓储（取本轴承在售关联）</param>
        /// <param name="merchantRepository">商户仓储（认证商家过滤用）</param>
        /// <param name="bearingId">关联平台轴承（搜索选中时有值）</param>
        /// <param name="partNumber">型号文本（无 bearingId 时兜底匹配）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async Task<IReadOnlyList<Guid>> GetMatchedMerchantIdsAsync(
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            Guid? bearingId, string? partNumber, CancellationToken cancellationToken = default)
        {
            // 优先用关联轴承；无 bearingId 时按型号文本精确匹配主数据兜底
            var resolvedId = bearingId;
            if (!resolvedId.HasValue && !string.IsNullOrWhiteSpace(partNumber))
            {
                var bearing = await bearingRepository.GetByPartNumberAsync(partNumber.Trim(), cancellationToken);
                resolvedId = bearing?.Id;
            }
            // 未命中主数据（长尾自由型号，无人有该型号在售）
            if (!resolvedId.HasValue)
                return new List<Guid>();

            var listings = await merchantBearingRepository.GetByBearingAsync(resolvedId.Value, cancellationToken);
            // 公域可见 = 审核通过 且（出售中 或 补货中）
            var candidates = listings
                .Where(mb => mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking))
                .Select(mb => mb.MerchantId)
                .Distinct()
                .ToList();

            var matched = new List<Guid>();
            foreach (var merchantId in candidates)
            {
                var merchant = await merchantRepository.GetByIdAsync(merchantId, cancellationToken);
                // 仅认证商家（可信数据）计入匹配；未认证商家不打扰、也不计入"已通知"家数
                if (merchant?.IsVerified == true)
                    matched.Add(merchantId);
            }
            return matched;
        }
    }
}

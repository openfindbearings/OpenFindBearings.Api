using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 寻货匹配器（v1.8.1 匹配口径统一）：集中"本单应推送给哪些商户"的唯一口径，
    /// 供发布处理器使用（解析最终收件人集合、回传推送家数）。
    /// 口径：公域可见（审核通过 且 出售中/补货中 且 未停用）本轴承的关联商户，去重后仅保留生效且已认证商家（可信数据）。
    /// 改动说明（v1.8.1）：改为复用实体上的"公域可见"唯一判定与商户状态判定——此前匹配器自写
    ///   "审核通过 且（出售中/补货中）"，而 matched/opportunities 查询只认出售中，两处口径漂移。
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
        /// <param name="excludeMerchantIds">需排除的商户集合（v1.57.3：发布人在职的全部商户 + 商户名义发布的商户，避免"自己通知自己"）</param>
        public static async Task<IReadOnlyList<Guid>> GetMatchedMerchantIdsAsync(
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            Guid? bearingId, string? partNumber, CancellationToken cancellationToken = default,
            IReadOnlyCollection<Guid>? excludeMerchantIds = null)
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
            var candidates = listings
                .Where(mb => mb.IsPubliclyVisible)
                .Select(mb => mb.MerchantId)
                // 改动说明（v1.57.3 自交易封堵）：排除集合由"单个发布商户"扩为"发布人在职的全部商户"，
                //   一人多商户时其余自家店铺同样不匹配、不通知、不可被邀请
                .Where(id => excludeMerchantIds == null || !excludeMerchantIds.Contains(id))
                .Distinct()
                .ToList();

            var matched = new List<Guid>();
            foreach (var merchantId in candidates)
            {
                var merchant = await merchantRepository.GetByIdAsync(merchantId, cancellationToken);
                // 仅生效且已认证商家（可信数据）计入匹配；未认证/已停用商家不打扰、也不计入推送家数
                if (merchant is { IsVerified: true, Status: MerchantStatus.Active })
                    matched.Add(merchantId);
            }
            return matched;
        }
    }
}

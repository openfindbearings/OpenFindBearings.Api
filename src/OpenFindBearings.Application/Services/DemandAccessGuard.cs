using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 寻货单管理权限守卫（v2.23.0 商户管理员代办）：集中"谁能管理一张寻货单"的唯一判定，
    /// 供取消、转公开、选定应答、批量删除、需求置顶等写操作复用。
    /// 口径：经办人本人恒可管理；商户名义单额外放行给"当前商户即发布商户 且 操作人是该商户在职管理员"，
    /// 修复经办人离职/停用后单据无人可管（删不掉、转不了公开、选不了应答）的死单问题。
    /// 个人名义单不适用第二档放行——个人需求与他人无关。
    /// </summary>
    public static class DemandAccessGuard
    {
        /// <summary>
        /// 判断操作人是否有权管理该寻货单。
        /// 先比经办人，再比"商户名义 + 当前商户管理员"，两者皆不满足即拒绝。
        /// </summary>
        /// <param name="demand">目标寻货单</param>
        /// <param name="userId">操作人用户 ID</param>
        /// <param name="actorMerchantId">当前商户上下文（请求头 X-Merchant-Id 解析，无则空）</param>
        /// <param name="memberRepository">商户成员仓储（校验在职管理员）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async Task<bool> CanManageAsync(
            SourcingDemand demand,
            Guid userId,
            Guid? actorMerchantId,
            IMerchantMemberRepository memberRepository,
            CancellationToken cancellationToken = default)
        {
            // 经办人本人：恒可管理（含个人名义单）
            if (demand.PublisherUserId == userId)
                return true;

            // 非商户名义单不存在代办场景
            if (demand.PublisherMerchantId is not Guid publisherMerchantId)
                return false;

            // 代办要求"当前商户恰为发布商户"，避免跨商户越权
            if (actorMerchantId != publisherMerchantId)
                return false;

            // 仅该商户在职管理员可代办普通成员经办的单据
            var member = await memberRepository.GetActiveByUserAndMerchantAsync(
                userId, publisherMerchantId, cancellationToken);
            return member is { IsAdmin: true };
        }
    }
}

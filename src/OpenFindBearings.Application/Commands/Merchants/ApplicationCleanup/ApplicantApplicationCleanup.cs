using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup
{
    /// <summary>
    /// 申请人放弃入驻申请（撤回/删除被拒）的共用清理逻辑（v2.6.0 提取）。
    /// 改动说明：withdraw（Pending）与 delete-application（Suspended）两个命令的
    /// 按渠道分支清理完全同构，此前若复制两份会随字段演进漂移，提取为单一实现。
    /// Self 硬删商户+成员、Claim 解除认领人并退回爬虫认领池，仓储变更由 UnitOfWork 统一提交。
    /// </summary>
    internal static class ApplicantApplicationCleanup
    {
        /// <summary>
        /// self 通道：硬删除商户本体 + 其全部成员行（外键 Restrict 须先删成员）。
        /// 未公示的草稿式商户放弃后彻底删除，避免残留同名/同代码记录干扰下次新建查重。
        /// 营业执照/商品等子表由 MerchantId 外键级联在 DB 层随商户删除一并清理。
        /// </summary>
        public static async Task HardDeleteMerchantWithMembersAsync(
            Merchant merchant,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            CancellationToken cancellationToken)
        {
            var members = await merchantMemberRepository.GetAllByMerchantIdAsync(merchant.Id, cancellationToken);
            foreach (var m in members)
            {
                await merchantMemberRepository.RemoveAsync(m, cancellationToken);
            }
            await merchantRepository.RemoveAsync(merchant, cancellationToken);
        }

        /// <summary>
        /// claim 通道：不删商户（本就属于爬虫/平台），仅软移除认领人成员关系，
        /// 并把商户来源退回 Crawler、渠道归 None，使其重新进入认领池且可被 Sync 覆盖。
        /// </summary>
        public static async Task RemoveClaimAndRevertToCrawlerAsync(
            Merchant merchant,
            MerchantMember applicantMember,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            CancellationToken cancellationToken)
        {
            applicantMember.Remove();
            await merchantMemberRepository.UpdateAsync(applicantMember, cancellationToken);

            merchant.RevertClaimedToCrawler("apply-abandon");
            await merchantRepository.UpdateAsync(merchant, cancellationToken);
        }
    }
}

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

        /// <summary>
        /// 商户被真人接管（认领/接受提名）时的经营性数据重置（v2.16.0）：
        /// 1. 在售商品关联清空——爬虫抓的价格/库存/MOQ 未经核实，继承会误导寻货买家，
        ///    且新认领人在商品管理页逐条删比重新录入更烦（对标高德/美团认领后商品从零自建）；
        /// 2. 旧证照材料清空——上一任认领人（被拒重开场景）的营业执照属其个人申请资料，
        ///    留给新认领人/Admin 抽屉可见是隐私泄露；
        /// 3. 待确认邀请作废（StaffJoin+Nomination 全类型）——旧认领人发出的邀请若被接受，
        ///    邀请人将成为新认领人商户的成员，必须随接管失效。
        /// 关注关系不清（属买家用户自己的数据）；基础资料由认领表单逐项核对，不在本方法职责。
        /// 仓储变更由 UnitOfWork 统一提交。
        /// </summary>
        public static async Task ResetOperationalDataForTakeoverAsync(
            Guid merchantId,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantDocumentRepository documentRepository,
            IStaffInvitationRepository invitationRepository,
            CancellationToken cancellationToken)
        {
            await merchantBearingRepository.DeleteByMerchantAsync(merchantId, cancellationToken);
            await documentRepository.DeleteByMerchantAsync(merchantId, cancellationToken);

            var pendingInvitations = await invitationRepository.GetPendingByMerchantAsync(merchantId, cancellationToken);
            foreach (var invitation in pendingInvitations)
            {
                invitation.Revoke();
                await invitationRepository.UpdateAsync(invitation, cancellationToken);
            }
        }
    }
}

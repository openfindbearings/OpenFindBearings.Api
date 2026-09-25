using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
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
        /// 改动说明（v2.17.0）：先硬删 Merchant 纠错行——CorrectionRequest 对 Merchants.TargetId
        ///   挂 Restrict FK，有纠错记录（含历史已审）的商户 DELETE 必被 23503 拦截，
        ///   withdraw/删被拒申请/账户注销三条既有路径同雷一并修复。
        /// 改动说明（v1.34.0 审计 U5 修复）：补删 MerchantDocument 证照行——原注释声称
        ///   "证照随 MerchantId 外键级联清理"是失实的（该 FK 配置已注释，MerchantBearing 有
        ///   级联成立、MerchantDocument 不成立），商户物理删除后证照行成孤儿、
        ///   FileUrl 指向 MinIO 的营业执照影像永久无主；接管/释放路径本就显式清证照，
        ///   此路径对齐。MinIO 对象删除列入 backlog（需存储层按 URL 反查键）
        /// </summary>
        public static async Task HardDeleteMerchantWithMembersAsync(
            Merchant merchant,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantDocumentRepository documentRepository,
            CancellationToken cancellationToken)
        {
            await correctionRepository.DeleteByTargetAsync("Merchant", merchant.Id, cancellationToken);
            // 证照行显式硬删（FK 级联不成立，见方法注释）
            await documentRepository.DeleteByMerchantAsync(merchant.Id, cancellationToken);
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

        /// <summary>
        /// 商户解除归属（关店/Admin 强制 detach）清场重置（v2.17.0）——与"接管"ResetOperationalDataForTakeoverAsync
        /// 语义分叉：接管=新主人进场清旧账，释放=全员离场归公海。差异点：
        /// 1. 成员行全量清退（接管不动成员——认领人即新主人；释放必须清，否则认领门"有在职成员"永久挡住再认领）；
        /// 2. 商品关联全删含爬虫行（历史爬虫关联释放后 staging 不会重推回填，保留=残缺混合体；
        ///    ProductCount 归零由 Merchant.ReleaseToPool 负责）；
        /// 3. 证照材料全删；
        /// 4. 非终态邀请全作废——含 Accepted 提名行（接管只清 Pending；Accepted 提名残留会在公海商户
        ///    被新认领人审批通过时，把旧提名的被提名人+发起人自动插回新认领人商户当成员——越权雷）；
        /// 5. Merchant 纠错行全量硬删（TargetId Restrict FK + 无主商户的纠错无人可续审）。
        /// 返回全部在职成员 userId 清单：调用方用于发"商户已关闭"站内信——领域事件在 commit 后派发，
        /// 届时查在职成员必空，必须在本方法内预取。仓储变更由 UnitOfWork 统一提交。
        /// </summary>
        public static async Task<List<Guid>> ResetOperationalDataForReleaseAsync(
            Guid merchantId,
            IMerchantMemberRepository memberRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantDocumentRepository documentRepository,
            IStaffInvitationRepository invitationRepository,
            ICorrectionRequestRepository correctionRepository,
            CancellationToken cancellationToken)
        {
            // 1. 成员全清退（先取在职名单作通知收件人）
            var members = await memberRepository.GetAllByMerchantIdAsync(merchantId, cancellationToken);
            var notifyUserIds = members
                .Where(m => m.Status == MerchantMemberStatus.Active)
                .Select(m => m.UserId).Distinct().ToList();
            foreach (var member in members)
            {
                if (member.Status == MerchantMemberStatus.Removed) continue;
                member.Remove();
                await memberRepository.UpdateAsync(member, cancellationToken);
            }

            // 2/3. 商品与证照全删（ExecuteDelete 批量，认领人真人来源与爬虫来源一并清）
            await merchantBearingRepository.DeleteByMerchantAsync(merchantId, cancellationToken);
            await documentRepository.DeleteByMerchantAsync(merchantId, cancellationToken);

            // 4. 非终态邀请全作废（Pending + Accepted 提名复活雷）
            var openInvitations = await invitationRepository.GetOpenByMerchantAsync(merchantId, cancellationToken);
            foreach (var invitation in openInvitations)
            {
                invitation.Revoke();
                await invitationRepository.UpdateAsync(invitation, cancellationToken);
            }

            // 5. 纠错行硬删（含历史已审，随商户离场归档删除；提交人"我的纠错"记录消失属注销级清场语义）
            await correctionRepository.DeleteByTargetAsync("Merchant", merchantId, cancellationToken);

            return notifyUserIds;
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.WithdrawApplication
{
    /// <summary>
    /// 申请人自助撤回入驻申请处理器。
    /// 前置校验：商户必须处于 Pending，且调用者是它当前在职的 MerchantAdmin 成员（即申请人本人）。
    /// 按 ApplicationMode 分支清理：Self 硬删商户及其成员；Claim 解除认领人成员并把来源退回爬虫。
    /// </summary>
    public class WithdrawApplicationCommandHandler : IRequestHandler<WithdrawApplicationCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<WithdrawApplicationCommandHandler> _logger;

        public WithdrawApplicationCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<WithdrawApplicationCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException("要撤回的申请不存在");
            }

            // 仅待审核可申请可撤回；通过后即为正式商户（走正常管理，不属"撤回"语义）
            if (merchant.Status != MerchantStatus.Pending)
            {
                throw new InvalidOperationException("仅待审核的入驻申请可以撤回");
            }

            // 撤回者必须是该商户当前在职管理员成员（申请提交时唯一的管理员即申请人），防止越权撤他人申请
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.ApplicantUserId, request.MerchantId, cancellationToken);
            if (member == null || !member.IsAdmin)
            {
                throw new InvalidOperationException("无权撤回该申请");
            }

            switch (merchant.ApplicationMode)
            {
                case ApplicationMode.Self:
                    await WithdrawSelfAsync(merchant, cancellationToken);
                    break;
                case ApplicationMode.Claim:
                    await WithdrawClaimAsync(merchant, member, cancellationToken);
                    break;
                default:
                    // Nomination / None 不走此自助撤回入口（提名发起方非成员、历史数据不该出现在申请列表）
                    throw new InvalidOperationException("该申请暂不支持自助撤回");
            }

            _logger.LogInformation("入驻申请已撤回: MerchantId={MerchantId}, Mode={Mode}, Applicant={UserId}",
                merchant.Id, merchant.ApplicationMode, request.ApplicantUserId);
        }

        /// <summary>
        /// self 新建撤回：硬删除商户本体 + 其全部成员行（外键 Restrict 须先删成员）。
        /// 改动说明：未公示的草稿式 Pending 商户撤回后彻底删除，避免残留同名/同代码记录干扰下次新建查重。
        /// 营业执照/商品等子表由 MerchantId 外键级联在 DB 层随商户删除一并清理。
        /// </summary>
        private async Task WithdrawSelfAsync(Domain.Aggregates.Merchant merchant, CancellationToken cancellationToken)
        {
            var members = await _merchantMemberRepository.GetAllByMerchantIdAsync(merchant.Id, cancellationToken);
            foreach (var m in members)
            {
                await _merchantMemberRepository.RemoveAsync(m, cancellationToken);
            }
            await _merchantRepository.RemoveAsync(merchant, cancellationToken);
        }

        /// <summary>
        /// claim 认领撤回：不删商户（本就属于爬虫/平台），仅软移除认领人成员关系，
        /// 并把商户来源退回 Crawler、渠道归 None，使其重新进入认领池且可被 Sync 覆盖。
        /// </summary>
        private async Task WithdrawClaimAsync(
            Domain.Aggregates.Merchant merchant,
            Domain.Entities.MerchantMember applicantMember,
            CancellationToken cancellationToken)
        {
            applicantMember.Remove();
            await _merchantMemberRepository.UpdateAsync(applicantMember, cancellationToken);

            merchant.RevertClaimedToCrawler("apply-revert");
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);
        }
    }
}

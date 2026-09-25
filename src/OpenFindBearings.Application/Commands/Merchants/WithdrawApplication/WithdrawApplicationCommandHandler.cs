using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
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
        // v2.17.0：HardDelete 前需先清纠错行（TargetId Restrict FK），注入纠错仓储
        private readonly ICorrectionRequestRepository _correctionRepository;
        // v1.34.0（审计 U5）：HardDelete 前显式清证照行（MerchantId FK 级联不成立）
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<WithdrawApplicationCommandHandler> _logger;

        public WithdrawApplicationCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<WithdrawApplicationCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            // 改动说明（v1.34.0 修复）：原 ctor 漏注入 _correctionRepository（字段声明未赋值，
            //   走到 HardDelete 必 NRE），连同新增的证照仓储一并补齐
            _correctionRepository = correctionRepository;
            _documentRepository = documentRepository;
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
                    // v2.6.0 改动说明：分支清理逻辑提取至 ApplicantApplicationCleanup 与"删除被拒申请"共用，此处仅换调用
                    await ApplicantApplicationCleanup.HardDeleteMerchantWithMembersAsync(
                        merchant, _merchantRepository, _merchantMemberRepository, _correctionRepository, _documentRepository, cancellationToken);
                    break;
                case ApplicationMode.Claim:
                    await ApplicantApplicationCleanup.RemoveClaimAndRevertToCrawlerAsync(
                        merchant, member, _merchantRepository, _merchantMemberRepository, cancellationToken);
                    break;
                default:
                    // Nomination / None 不走此自助撤回入口（提名发起方非成员、历史数据不该出现在申请列表）
                    throw new InvalidOperationException("该申请暂不支持自助撤回");
            }

            _logger.LogInformation("入驻申请已撤回: MerchantId={MerchantId}, Mode={Mode}, Applicant={UserId}",
                merchant.Id, merchant.ApplicationMode, request.ApplicantUserId);
        }
    }
}

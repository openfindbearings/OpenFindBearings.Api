using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.DeleteRejectedApplication
{
    /// <summary>
    /// 申请人删除被驳回入驻申请处理器（v2.6.0 新增）。
    /// 与撤回的差异仅在状态守卫（Suspended vs Pending），数据清理复用共用逻辑；
    /// 被拒商户拒绝时已被 Deactivate 标记软删，Self 通道删除走物理硬删（仓储 RemoveAsync 既有语义）。
    /// </summary>
    public class DeleteRejectedApplicationCommandHandler : IRequestHandler<DeleteRejectedApplicationCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        // v2.17.0：HardDelete 前需先清纠错行（TargetId Restrict FK），注入纠错仓储
        private readonly ICorrectionRequestRepository _correctionRepository;
        // v1.34.0（审计 U5）：HardDelete 前显式清证照行（MerchantId FK 级联不成立）
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<DeleteRejectedApplicationCommandHandler> _logger;

        public DeleteRejectedApplicationCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ICorrectionRequestRepository correctionRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<DeleteRejectedApplicationCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            // 改动说明（v1.34.0 修复）：原 ctor 漏注入 _correctionRepository（字段声明未赋值，
            //   走到 HardDelete 必 NRE），连同新增的证照仓储一并补齐
            _correctionRepository = correctionRepository;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteRejectedApplicationCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException("要删除的申请不存在");
            }

            // 仅被驳回的申请可删：审核中用"撤回"、生效后是正式商户（走商户管理），三态语义不混用
            if (merchant.Status != MerchantStatus.Suspended)
            {
                throw new InvalidOperationException("仅被驳回的入驻申请可以删除");
            }

            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.ApplicantUserId, request.MerchantId, cancellationToken);
            if (member == null || !member.IsAdmin)
            {
                throw new InvalidOperationException("无权删除该申请");
            }

            switch (merchant.ApplicationMode)
            {
                case ApplicationMode.Self:
                    await ApplicantApplicationCleanup.HardDeleteMerchantWithMembersAsync(
                        merchant, _merchantRepository, _merchantMemberRepository, _correctionRepository, _documentRepository, cancellationToken);
                    break;
                case ApplicationMode.Claim:
                    await ApplicantApplicationCleanup.RemoveClaimAndRevertToCrawlerAsync(
                        merchant, member, _merchantRepository, _merchantMemberRepository, cancellationToken);
                    break;
                default:
                    // 提名被拒后商户仍归发起人邀请流程管理；None 为历史/爬虫数据无申请人，均不开放自助删除
                    throw new InvalidOperationException("该申请暂不支持自助删除");
            }

            _logger.LogInformation("被拒入驻申请已删除: MerchantId={MerchantId}, Mode={Mode}, Applicant={UserId}",
                merchant.Id, merchant.ApplicationMode, request.ApplicantUserId);
        }
    }
}

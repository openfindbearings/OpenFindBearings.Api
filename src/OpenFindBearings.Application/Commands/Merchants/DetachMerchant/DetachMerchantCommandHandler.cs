using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.Merchants.DetachMerchant
{
    /// <summary>
    /// Admin 强制解除归属处理器（v2.17.0）。仅对"有公海数据可回"的商户生效：
    /// claim 或提名已有商家（ApplicationMode=None 但 DataSource=Manual）→ release；
    /// self/提名新建商户无 staging 行可唤醒，拒绝 detach（应走 Admin 删除，避免幽灵公海商户）。
    /// 清场复用 ResetOperationalDataForReleaseAsync + ReleaseToPool，全员在职成员收站内信。
    /// </summary>
    public class DetachMerchantCommandHandler : IRequestHandler<DetachMerchantCommand, DetachMerchantResult>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DetachMerchantCommandHandler> _logger;

        public DetachMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantDocumentRepository documentRepository,
            IStaffInvitationRepository invitationRepository,
            ICorrectionRequestRepository correctionRepository,
            INotificationService notificationService,
            ILogger<DetachMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _documentRepository = documentRepository;
            _invitationRepository = invitationRepository;
            _correctionRepository = correctionRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<DetachMerchantResult> Handle(DetachMerchantCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商户不存在");

            if (merchant.Status != MerchantStatus.Active)
            {
                throw new InvalidOperationException("仅生效商户可解除归属");
            }

            // 通道守卫：self/提名新建无公海数据，release 会造永不被爬虫裁判的幽灵商户
            if (merchant.ApplicationMode is ApplicationMode.Self or ApplicationMode.Nomination)
            {
                throw new InvalidOperationException("自助创建或提名新建的商户无公海数据可退回，请使用删除操作");
            }
            if (merchant.DataSource?.SourceType != DataSourceType.Manual)
            {
                throw new InvalidOperationException("该商户未被真人认领，无需解除归属");
            }

            // 清场 + 归属轴重置（与自助关店 release 分支同构）
            var notifyUserIds = await ApplicantApplicationCleanup.ResetOperationalDataForReleaseAsync(
                merchant.Id, _memberRepository, _merchantBearingRepository, _documentRepository,
                _invitationRepository, _correctionRepository, cancellationToken);

            merchant.ReleaseToPool();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            var reasonPart = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $"（原因：{request.Reason}）";
            var body = $"商户「{merchant.Name}」已被平台解除归属{reasonPart}，退回公开信息池；您的成员关系已解除，如为误操作可重新认领。";
            foreach (var userId in notifyUserIds)
            {
                await _notificationService.AddInAppAsync(
                    userId, Notification.TypeMerchantClosed, "商户解除归属通知", body,
                    Notification.BizMerchant, merchant.Id, cancellationToken);
            }

            _logger.LogInformation("Admin 强制解除归属完成: MerchantId={Id} Notified={Count}",
                merchant.Id, notifyUserIds.Count);

            return new DetachMerchantResult(true, merchant.Name);
        }
    }
}

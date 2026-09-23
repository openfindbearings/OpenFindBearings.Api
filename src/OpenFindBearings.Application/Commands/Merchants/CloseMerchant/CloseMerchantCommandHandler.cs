using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.Merchants.CloseMerchant
{
    /// <summary>
    /// 商户自助关店处理器（v2.17.0）。守卫：商户 Active + 发起人为在职管理员（任一即可，
    /// 无需全体同意——损失可逆，其余管理员收站内信后可重新认领）。
    /// 通道二分：
    /// - self / 提名新建（ApplicationMode=Nomination 仅新建时打）→ 删除分支：无公海数据可回，
    ///   复用 HardDeleteMerchantWithMembersAsync 整删（含纠错行 FK 共享修复），粉丝随 Cascade 删除；
    /// - claim / 提名已有商家（ApplicationMode=None 但 DataSource=Manual——提名已有不打模式标记）→
    ///   release 分支：ReleaseToPool + 清场 helper，商户回公海待认领。
    /// 通知：helper 预取在职成员名单（事件 commit 后派发届时查必空），逐人 AddInAppAsync。
    /// Sync refresh 由端点层在命令成功后 best-effort 调用（失败仅日志+可手动补，不回滚关店）。
    /// </summary>
    public class CloseMerchantCommandHandler : IRequestHandler<CloseMerchantCommand, CloseMerchantResult>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CloseMerchantCommandHandler> _logger;

        public CloseMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantDocumentRepository documentRepository,
            IStaffInvitationRepository invitationRepository,
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            ILogger<CloseMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _documentRepository = documentRepository;
            _invitationRepository = invitationRepository;
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<CloseMerchantResult> Handle(CloseMerchantCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商户不存在");

            if (merchant.Status != MerchantStatus.Active)
            {
                throw new InvalidOperationException("仅生效商户可关店：审核中请撤回申请，被驳回请删除申请");
            }

            // 发起人守卫：必须是在职管理员（员工无关店权，对标美团子账号/Amazon co-manager）
            var operatorMember = (await _memberRepository.GetActiveByMerchantAsync(merchant.Id, cancellationToken))
                .FirstOrDefault(m => m.UserId == request.OperatorUserId);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new InvalidOperationException("仅商户管理员可执行关店");
            }

            // 通道二分：self/提名新建无公海数据可回→删除；claim/提名已有→release 回公海
            var isDeleteBranch = merchant.ApplicationMode is ApplicationMode.Self or ApplicationMode.Nomination;

            var notifyUserIds = (await _memberRepository.GetActiveByMerchantAsync(merchant.Id, cancellationToken))
                .Select(m => m.UserId).Distinct().ToList();

            string result;
            if (isDeleteBranch)
            {
                // 删除分支：成员/纠错行由共享 helper 清理，商品/证照/关注随 DB 级联
                await ApplicantApplicationCleanup.HardDeleteMerchantWithMembersAsync(
                    merchant, _merchantRepository, _memberRepository, _correctionRepository, cancellationToken);
                result = "deleted";
            }
            else
            {
                // release 分支：清场 helper（成员全清/商品/证照/全部邀请含 Accepted 提名/纠错硬删）
                await ApplicantApplicationCleanup.ResetOperationalDataForReleaseAsync(
                    merchant.Id, _memberRepository, _merchantBearingRepository, _documentRepository,
                    _invitationRepository, _correctionRepository, cancellationToken);
                // 归属轴重置（DataSource 回 Crawler/认证与核验轴清/Contact 隐私止血/ProductCount 归零）
                merchant.ReleaseToPool();
                await _merchantRepository.UpdateAsync(merchant, cancellationToken);
                result = "released";
            }

            // 全员在职成员站内信（发起人自己也在名单内，作为结果确认）
            var operatorUser = await _userRepository.GetByIdAsync(request.OperatorUserId, cancellationToken);
            var operatorName = string.IsNullOrWhiteSpace(operatorUser?.Nickname) ? "管理员" : operatorUser!.Nickname;
            var body = isDeleteBranch
                ? $"商户「{merchant.Name}」已被{operatorName}关闭，在售信息与关注关系已一并删除，此操作不可恢复。"
                : $"商户「{merchant.Name}」已被{operatorName}关闭并退回公开信息池，资料将随互联网数据自然更新，可搜索重新认领。";
            foreach (var userId in notifyUserIds)
            {
                await _notificationService.AddInAppAsync(
                    userId, Notification.TypeMerchantClosed, "关店通知", body,
                    Notification.BizMerchant, merchant.Id, cancellationToken);
            }

            _logger.LogInformation("商户关店完成: MerchantId={Id} Mode={Result} Operator={Op} Notified={Count}",
                merchant.Id, result, request.OperatorUserId, notifyUserIds.Count);

            return new CloseMerchantResult(!isDeleteBranch, merchant.Name);
        }
    }
}

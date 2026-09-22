using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
using OpenFindBearings.Application.Exceptions;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.Users.DeactivateUser
{
    /// <summary>
    /// 注销账户处理器（v2.12.0）。两遍扫描：先守卫（唯一管理员拦截，避免半途改数据再抛），
    /// 再执行清理（申请撤回/成员移除/邀请作废/通知清空/软删标记/Identity 禁用吊销）。
    /// 任何一步抛异常由 UnitOfWork 整体回滚，不会留下半注销状态。
    /// </summary>
    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IIdentityService _identityService;
        private readonly ILogger<DeactivateUserCommandHandler> _logger;

        public DeactivateUserCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            IStaffInvitationRepository invitationRepository,
            INotificationRepository notificationRepository,
            IIdentityService identityService,
            ILogger<DeactivateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _invitationRepository = invitationRepository;
            _notificationRepository = notificationRepository;
            _identityService = identityService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException("用户不存在");
            if (user.DeactivatedAt.HasValue)
                throw new InvalidOperationException("该账户已注销");

            var members = await _memberRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

            // 第一遍守卫：生效商户的唯一在职管理员不允许注销（商户会变无主孤儿）
            var blockingNames = new List<string>();
            foreach (var member in members.Where(m => m.IsAdmin))
            {
                var merchant = await _merchantRepository.GetByIdAsync(member.MerchantId, cancellationToken);
                if (merchant == null) continue;
                // 审核中/被拒的本人申请走撤回清理，不算拦截项；提名通道同理放行（被提名人未入伙）
                if (merchant.Status is MerchantStatus.Pending or MerchantStatus.Suspended) continue;
                if (await _memberRepository.CountActiveAdminsAsync(merchant.Id, cancellationToken) <= 1)
                {
                    blockingNames.Add(merchant.Name);
                }
            }
            if (blockingNames.Count > 0)
            {
                throw new InvalidOperationException(
                    $"您仍是商户「{string.Join("、", blockingNames)}」的唯一管理员，请先在成员管理中转让管理员，或联系平台注销该商户后再注销账户");
            }

            // 第二遍执行：按商户状态分流清理
            foreach (var member in members)
            {
                var merchant = await _merchantRepository.GetByIdAsync(member.MerchantId, cancellationToken);
                if (merchant == null) continue;

                if (merchant.Status == MerchantStatus.Pending && member.IsAdmin
                    && merchant.ApplicationMode is ApplicationMode.Self or ApplicationMode.Claim)
                {
                    // 审核中的本人申请：等同撤回（Self 硬删商户+成员；Claim 解除认领退回数据池）
                    await CleanupApplicationAsync(merchant, member, cancellationToken);
                    continue;
                }
                if (merchant.Status == MerchantStatus.Suspended && member.IsAdmin
                    && merchant.ApplicationMode is ApplicationMode.Self or ApplicationMode.Claim)
                {
                    // 被拒的本人申请：等同删除申请清理
                    await CleanupApplicationAsync(merchant, member, cancellationToken);
                    continue;
                }

                // 其余（生效商户的多管理员之一 / 员工 / 提名通道成员）：移除成员行
                member.Remove();
                await _memberRepository.UpdateAsync(member, cancellationToken);
            }

            // 作废发给本人的待确认员工邀请（人走了邀请无意义）
            if (!string.IsNullOrWhiteSpace(request.Phone) || !string.IsNullOrWhiteSpace(request.Email))
            {
                var invitations = await _invitationRepository.GetPendingStaffInvitationsByContactAsync(
                    request.Phone, request.Email, cancellationToken);
                foreach (var invitation in invitations)
                {
                    invitation.Revoke();
                    await _invitationRepository.UpdateAsync(invitation, cancellationToken);
                }
            }

            // 清空个人通知数据
            await _notificationRepository.DeleteAllForUserAsync(request.UserId, cancellationToken);

            // 软删标记（冷静期起点）+ Identity 禁用与全设备令牌吊销（失败抛异常整体回滚）
            user.Deactivate();
            await _userRepository.UpdateAsync(user, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.AuthUserId)
                && !await _identityService.DeactivateUserAsync(request.AuthUserId, cancellationToken))
            {
                throw new InvalidOperationException("认证服务注销失败，请稍后重试");
            }

            _logger.LogInformation("账户已注销（进入 30 天冷静期）: UserId={UserId}", request.UserId);
        }

        /// <summary>
        /// 按入驻渠道复用撤回/删拒共用的清理逻辑（Self 硬删、Claim 退回数据池）
        /// </summary>
        private async Task CleanupApplicationAsync(Merchant merchant, MerchantMember member, CancellationToken cancellationToken)
        {
            if (merchant.ApplicationMode == ApplicationMode.Self)
            {
                await ApplicantApplicationCleanup.HardDeleteMerchantWithMembersAsync(
                    merchant, _merchantRepository, _memberRepository, cancellationToken);
            }
            else
            {
                await ApplicantApplicationCleanup.RemoveClaimAndRevertToCrawlerAsync(
                    merchant, member, _merchantRepository, _memberRepository, cancellationToken);
            }
        }
    }
}

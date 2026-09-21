using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.StaffInvitationActions
{
    /// <summary>
    /// 接受员工邀请处理器：校验（Type=Staff + Pending + 有效期内 + 手机号服务端匹配）→
    /// 建成员行（显式仓储 Add，绕开 EF 导航自动发现陷阱）→ 邀请 Complete → 站内信通知发起人
    /// </summary>
    public class AcceptStaffInvitationCommandHandler : IRequestHandler<AcceptStaffInvitationCommand, bool>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AcceptStaffInvitationCommandHandler> _logger;

        public AcceptStaffInvitationCommandHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantMemberRepository memberRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            ILogger<AcceptStaffInvitationCommandHandler> logger)
        {
            _invitationRepository = invitationRepository;
            _memberRepository = memberRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> Handle(AcceptStaffInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
                ?? throw new KeyNotFoundException("邀请不存在");

            if (invitation.Type != InvitationType.Staff)
                throw new InvalidOperationException("该邀请不是员工邀请");
            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("邀请已处理或已失效");
            if (invitation.IsExpired())
                throw new InvalidOperationException("邀请已过期");
            // 改动说明：手机号或邮箱任一"非空且相等"才算本人（服务端 JWT claim），防撞领他人邀请；
            //   必须排除 null==null 穿透（邀请只有 phone 而用户无 email 时双 null 会误放行）
            var phoneMatch = !string.IsNullOrEmpty(request.Phone) && invitation.Phone == request.Phone;
            var emailMatch = !string.IsNullOrEmpty(request.Email) && invitation.Email == request.Email;
            if (!phoneMatch && !emailMatch)
                throw new UnauthorizedAccessException("该邀请不是发给您的");

            // 已是该商户在职成员 → 幂等拒绝（防接受后又被移除再接受的旧邀请复活）
            var existing = await _memberRepository.GetByUserAndMerchantAsync(request.UserId, invitation.MerchantId, cancellationToken);
            if (existing != null && existing.Status == MerchantMemberStatus.Active)
                throw new InvalidOperationException("您已是该商户成员");

            var role = invitation.Role ?? MerchantMember.RoleMerchantStaff;
            if (existing != null)
            {
                // 曾被移除留下 Removed 行 → 复用该行重新入伙（不建第二行，遵守唯一键）
                existing.Rejoin(role, invitation.OperatorId);
                await _memberRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var member = new MerchantMember(request.UserId, invitation.MerchantId, role, invitation.OperatorId);
                await _memberRepository.AddAsync(member, cancellationToken);
            }

            invitation.Complete(request.AuthUserId);
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            // 通知发起人：被邀人已同意入伙
            var merchant = await _merchantRepository.GetByIdAsync(invitation.MerchantId, cancellationToken);
            var newUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            await _notificationService.AddInAppAsync(
                invitation.OperatorId,
                Notification.TypeStaffJoinAccepted,
                "邀请已被接受",
                $"{newUser?.Nickname ?? "被邀请人"} 已接受邀请，加入商户「{merchant?.Name ?? "商家"}」{(role == MerchantMember.RoleMerchantAdmin ? "为管理员" : "为员工")}。",
                Notification.BizMerchant,
                invitation.MerchantId,
                cancellationToken);

            _logger.LogInformation("员工邀请已接受: InvitationId={Id}, UserId={UserId}, MerchantId={MerchantId}",
                invitation.Id, request.UserId, invitation.MerchantId);

            return true;
        }
    }
}

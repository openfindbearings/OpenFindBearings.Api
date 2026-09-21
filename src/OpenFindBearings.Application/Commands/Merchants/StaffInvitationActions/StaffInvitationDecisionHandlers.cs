using MediatR;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.StaffInvitationActions
{
    /// <summary>
    /// 拒绝员工邀请处理器：手机号服务端匹配 → 邀请置 Declined（不通知发起人，与主流协作产品一致）
    /// </summary>
    public class DeclineStaffInvitationCommandHandler : IRequestHandler<DeclineStaffInvitationCommand, bool>
    {
        private readonly IStaffInvitationRepository _invitationRepository;

        public DeclineStaffInvitationCommandHandler(IStaffInvitationRepository invitationRepository)
        {
            _invitationRepository = invitationRepository;
        }

        /// <inheritdoc/>
        public async Task<bool> Handle(DeclineStaffInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
                ?? throw new KeyNotFoundException("邀请不存在");

            if (invitation.Type != InvitationType.Staff)
                throw new InvalidOperationException("该邀请不是员工邀请");
            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("邀请已处理或已失效");
            // 改动说明（v2.9.0）：手机号或邮箱任一"非空且相等"才算本人（服务端 JWT claim），
            //   排除 null==null 穿透；邮箱注册无手机号用户由 email 兜底
            var phoneMatch = !string.IsNullOrEmpty(request.Phone) && invitation.Phone == request.Phone;
            var emailMatch = !string.IsNullOrEmpty(request.Email) && invitation.Email == request.Email;
            if (!phoneMatch && !emailMatch)
                throw new UnauthorizedAccessException("该邀请不是发给您的");

            invitation.Decline();
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// 撤销员工邀请处理器：操作人须为该商户在职管理员 → 邀请置 Revoked
    /// </summary>
    public class RevokeStaffInvitationCommandHandler : IRequestHandler<RevokeStaffInvitationCommand, bool>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantMemberRepository _memberRepository;

        public RevokeStaffInvitationCommandHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantMemberRepository memberRepository)
        {
            _invitationRepository = invitationRepository;
            _memberRepository = memberRepository;
        }

        /// <inheritdoc/>
        public async Task<bool> Handle(RevokeStaffInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
                ?? throw new KeyNotFoundException("邀请不存在");

            if (invitation.Type != InvitationType.Staff)
                throw new InvalidOperationException("该邀请不是员工邀请");
            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("邀请已处理或已失效");

            var operatorMember = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, invitation.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
                throw new UnauthorizedAccessException("需要商户管理员权限");

            invitation.Revoke();
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);
            return true;
        }
    }
}

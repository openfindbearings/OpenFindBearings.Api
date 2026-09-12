using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.ApproveMerchant
{
    /// <summary>
    /// 审核通过入驻申请命令处理器
    /// 将 Pending 状态的商户转为 Active，使其生效（补充原缺失的审核门：Approve 无调用方）
    /// 若该商户是提名模式（Nomination 邀请已接受），同时按 InitiatorJoins 建双方成员行
    /// </summary>
    public class ApproveMerchantCommandHandler : IRequestHandler<ApproveMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ApproveMerchantCommandHandler> _logger;

        public ApproveMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IStaffInvitationRepository invitationRepository,
            IUserRepository userRepository,
            ILogger<ApproveMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _invitationRepository = invitationRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过入驻申请: MerchantId={MerchantId}, ApprovedBy={ApprovedBy}",
                request.Id, request.ApprovedBy);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            merchant.Approve();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 提名模式商户审核通过：建被提名人（管理员）与发起人（员工）成员行
            await CreateNominationMembersAsync(merchant.Id, cancellationToken);

            _logger.LogInformation("入驻申请审核通过，商户已生效: MerchantId={MerchantId}", request.Id);
        }

        /// <summary>
        /// 提名商户审核通过时建成员行
        /// 被提名人 = MerchantAdmin（已接受邀请）；发起人 = MerchantStaff（InitiatorJoins=true 时）
        /// </summary>
        private async Task CreateNominationMembersAsync(Guid merchantId, CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetLatestByMerchantAndTypeAsync(
                merchantId, InvitationType.Nomination, cancellationToken);
            if (invitation == null || invitation.Status != InvitationStatus.Accepted)
                return;

            // 被提名人（管理员）
            if (!string.IsNullOrWhiteSpace(invitation.CompletedSub))
            {
                var nomineeUser = await _userRepository.GetByAuthUserIdAsync(invitation.CompletedSub, cancellationToken);
                if (nomineeUser != null)
                {
                    var nomineeMember = new MerchantMember(
                        nomineeUser.Id, merchantId, MerchantMember.RoleMerchantAdmin, invitation.OperatorId);
                    await _merchantMemberRepository.AddAsync(nomineeMember, cancellationToken);
                    _logger.LogInformation("提名被提名人成为管理员: UserId={UserId}, MerchantId={MerchantId}",
                        nomineeUser.Id, merchantId);
                }
            }

            // 发起人（员工，InitiatorJoins 默认入伙）
            if (invitation.InitiatorJoins && invitation.OperatorId != Guid.Empty)
            {
                var initiatorMember = new MerchantMember(
                    invitation.OperatorId, merchantId, MerchantMember.RoleMerchantStaff);
                await _merchantMemberRepository.AddAsync(initiatorMember, cancellationToken);
                _logger.LogInformation("提名发起人成为员工: UserId={UserId}, MerchantId={MerchantId}",
                    invitation.OperatorId, merchantId);
            }
        }
    }
}

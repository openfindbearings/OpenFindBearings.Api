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
    /// v2.7.0：入驻通过时把随单待审材料级联置 Approved（申请单一次审，材料不再单独排队）
    /// </summary>
    public class ApproveMerchantCommandHandler : IRequestHandler<ApproveMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ApproveMerchantCommandHandler> _logger;

        public ApproveMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IStaffInvitationRepository invitationRepository,
            IMerchantDocumentRepository documentRepository,
            IUserRepository userRepository,
            ILogger<ApproveMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _invitationRepository = invitationRepository;
            _documentRepository = documentRepository;
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

            // 改动说明：并发守卫——另一管理员已先处理时返回 409（领域守卫抛 400 语义不准，此处先行拦截）
            if (merchant.Status != MerchantStatus.Pending)
            {
                throw new OpenFindBearings.Application.Exceptions.MerchantAlreadyProcessedException(merchant.Status.ToString());
            }

            // v2.17.0 双保险：公海商户（爬虫来源）无入驻申请不可审批——防释放商户残留的
            //   Accepted 提名邀请经直调 approve 把旧提名人插回新商户成员（根源已由 detach 清 Accepted 封堵）
            if (merchant.DataSource?.SourceType == DataSourceType.Crawler)
            {
                throw new InvalidOperationException("公海商户（爬虫来源）无入驻申请，不可审批");
            }

            merchant.Approve();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 改动说明（v2.7.0）：申请单审核通过时随单待审材料级联批准（材料不独立排队，一次审结）；
            //   ApprovedBy 为字符串 claim 值，解析失败以 Guid.Empty 记录（不阻断主流程）
            var approverId = Guid.TryParse(request.ApprovedBy, out var parsedApprover) ? parsedApprover : Guid.Empty;
            var documents = await _documentRepository.GetByMerchantIdAsync(merchant.Id, cancellationToken);
            foreach (var doc in documents.Where(d => d.Status == DocumentStatus.Pending))
            {
                doc.Approve(approverId, "随入驻申请审核通过");
                await _documentRepository.UpdateAsync(doc, cancellationToken);
            }

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

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Merchants.NominateMerchant
{
    /// <summary>
    /// 提名他人为管理员命令处理器
    /// 建 Draft 商户 + Nomination 邀请，返回邀请码（链接指向 Taro H5 注册/登录页带 code）
    /// </summary>
    public class NominateMerchantCommandHandler : IRequestHandler<NominateMerchantCommand, string>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<NominateMerchantCommandHandler> _logger;

        public NominateMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IStaffInvitationRepository invitationRepository,
            ILogger<NominateMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _invitationRepository = invitationRepository;
            _logger = logger;
        }

        public async Task<string> Handle(NominateMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提名他人为管理员: Initiator={InitiatorUserId}, Nominee={NomineePhone}/{NomineeEmail}",
                request.InitiatorUserId, request.NomineePhone, request.NomineeEmail);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("商户名称不能为空");
            }
            if (string.IsNullOrWhiteSpace(request.NomineePhone) && string.IsNullOrWhiteSpace(request.NomineeEmail))
            {
                throw new InvalidOperationException("请填写被提名管理员的手机号或邮箱");
            }
            if (request.InitiatorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("请先登录后再发起入驻提名");
            }

            // 建 Draft 商户（未生效前不可被检索、不被 Sync 合并）
            var contact = new ContactInfo(
                request.ContactPerson,
                request.Phone,
                request.Mobile,
                request.Email,
                request.Address);

            var merchant = new Merchant(
                request.Name.Trim(),
                request.Type.HasValue ? (MerchantType)request.Type.Value : MerchantType.Trader,
                contact);
            merchant.MarkAsDraft();
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName,
                unifiedSocialCreditCode: null,
                description: null,
                businessScope: null,
                logoUrl: null,
                website: null);

            await _merchantRepository.AddAsync(merchant, cancellationToken);

            // 建 Nomination 邀请（Role=MerchantAdmin，InitiatorJoins 决定发起人是否默认入伙）
            var invitationCode = Guid.NewGuid().ToString("N")[..12];
            var invitation = new StaffInvitation(
                merchantId: merchant.Id,
                email: request.NomineeEmail,
                phone: request.NomineePhone,
                role: MerchantMember.RoleMerchantAdmin,
                invitationCode: invitationCode,
                operatorId: request.InitiatorUserId,
                type: InvitationType.Nomination,
                status: InvitationStatus.Pending,
                initiatorJoins: request.InitiatorJoins);
            await _invitationRepository.AddAsync(invitation, cancellationToken);

            // 送达：短信/邮件真实通道属 Identity P2 范畴，本期记录日志由站内/链接触达
            _logger.LogInformation("提名邀请已创建: Code={Code}, 邀请链接=taro H5 注册/登录页带 inviteCode", invitationCode);

            return invitationCode;
        }
    }
}

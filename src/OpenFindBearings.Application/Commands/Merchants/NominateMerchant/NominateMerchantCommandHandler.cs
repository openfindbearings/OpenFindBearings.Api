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
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<NominateMerchantCommandHandler> _logger;

        public NominateMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IStaffInvitationRepository invitationRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<NominateMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _invitationRepository = invitationRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        public async Task<string> Handle(NominateMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提名他人为管理员: Initiator={InitiatorUserId}, Nominee={NomineePhone}/{NomineeEmail}, Target={TargetMerchantId}",
                request.InitiatorUserId, request.NomineePhone, request.NomineeEmail, request.TargetMerchantId);

            if (string.IsNullOrWhiteSpace(request.NomineePhone) && string.IsNullOrWhiteSpace(request.NomineeEmail))
            {
                throw new InvalidOperationException("请填写被提名管理员的手机号或邮箱");
            }
            if (request.InitiatorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("请先登录后再发起入驻提名");
            }

            Guid merchantId;

            if (request.TargetMerchantId.HasValue && request.TargetMerchantId.Value != Guid.Empty)
            {
                // 提名认领已有商家：不新建、不改来源；校验目标仍可被认领（未认证 + 非草稿 + 无在职成员）。
                //   成员行仍由审核通过时创建（与提名新建一致），此处仅登记 Nomination 邀请。
                var existing = await _merchantRepository.GetByIdAsync(request.TargetMerchantId.Value, cancellationToken);
                if (existing == null)
                {
                    throw new InvalidOperationException("要提名的商家不存在");
                }
                if (existing.IsVerified)
                {
                    throw new InvalidOperationException("该商家已认证，无法提名认领");
                }
                if (existing.Status == MerchantStatus.Draft)
                {
                    throw new InvalidOperationException("草稿商家不可提名认领");
                }
                var existingMembers = await _merchantMemberRepository.GetActiveByMerchantAsync(existing.Id, cancellationToken);
                if (existingMembers.Count > 0)
                {
                    throw new InvalidOperationException("该商家已被认领");
                }
                merchantId = existing.Id;
            }
            else
            {
                // 提名新建：建 Draft 商户（未生效前不可被检索、不被 Sync 合并）
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new InvalidOperationException("商户名称不能为空");
                }

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
                merchantId = merchant.Id;
            }

            // 防重复提名：同一商家已有未过期的 Pending Nomination 时，不再叠加（避免多发起人/重复发）
            var latestNomination = await _invitationRepository.GetLatestByMerchantAndTypeAsync(
                merchantId, InvitationType.Nomination, cancellationToken);
            if (latestNomination != null && latestNomination.Status == InvitationStatus.Pending && !latestNomination.IsExpired())
            {
                throw new InvalidOperationException("该商家已有进行中的管理员提名，请等待对方处理");
            }

            // 建 Nomination 邀请（Role=MerchantAdmin，InitiatorJoins 决定发起人是否默认入伙）
            var invitationCode = Guid.NewGuid().ToString("N")[..12];
            var invitation = new StaffInvitation(
                merchantId: merchantId,
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
            _logger.LogInformation("提名邀请已创建: Code={Code}, MerchantId={MerchantId}, 邀请链接=taro H5 注册/登录页带 inviteCode", invitationCode, merchantId);

            return invitationCode;
        }
    }
}

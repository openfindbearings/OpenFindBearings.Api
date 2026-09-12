using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AcceptNomination
{
    /// <summary>
    /// 接受管理员提名命令处理器
    /// 校验邀请有效且未过期，补全商户资料并转为 Pending（Draft -> Pending）
    /// </summary>
    public class AcceptNominationCommandHandler : IRequestHandler<AcceptNominationCommand, Guid>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AcceptNominationCommandHandler> _logger;

        public AcceptNominationCommandHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantRepository merchantRepository,
            ILicenseVerificationRepository licenseRepository,
            IUserRepository userRepository,
            ILogger<AcceptNominationCommandHandler> logger)
        {
            _invitationRepository = invitationRepository;
            _merchantRepository = merchantRepository;
            _licenseRepository = licenseRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(AcceptNominationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("接受管理员提名: Code={Code}, Nominee={NomineeUserId}",
                request.InvitationCode, request.NomineeUserId);

            if (request.NomineeUserId == Guid.Empty)
            {
                throw new InvalidOperationException("请先登录后再接受提名");
            }

            var invitation = await _invitationRepository.GetByCodeAsync(request.InvitationCode, cancellationToken);
            if (invitation == null || invitation.Type != InvitationType.Nomination)
            {
                throw new InvalidOperationException("提名邀请无效");
            }
            if (invitation.Status != InvitationStatus.Pending)
            {
                throw new InvalidOperationException("提名邀请已处理，无法重复接受");
            }
            if (invitation.IsExpired())
            {
                invitation.MarkExpired();
                await _invitationRepository.UpdateAsync(invitation, cancellationToken);
                throw new InvalidOperationException("提名邀请已过期，请联系发起人重新提名");
            }

            // 安全强化：邀请指定了手机号时，仅该手机号的登录用户可接受（防邀请码泄露被冒领）
            if (!string.IsNullOrWhiteSpace(invitation.Phone) &&
                !string.Equals(invitation.Phone, request.NomineePhone, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("该提名邀请仅限被提名人本人接受");
            }

            var merchant = await _merchantRepository.GetByIdAsync(invitation.MerchantId, cancellationToken);
            if (merchant == null || merchant.Status != MerchantStatus.Draft)
            {
                throw new InvalidOperationException("提名商户状态异常，无法接受");
            }

            // 修复 B3：补资料场景做字段级合并（?? 原值），
            // 被提名人未填的字段不清空提名（Draft）阶段已填的值
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName ?? merchant.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode ?? merchant.UnifiedSocialCreditCode,
                description: request.Description ?? merchant.Description,
                businessScope: merchant.BusinessScope,
                logoUrl: merchant.LogoUrl,
                website: merchant.Website);

            if (!string.IsNullOrWhiteSpace(request.ContactPerson) ||
                !string.IsNullOrWhiteSpace(request.Phone) ||
                !string.IsNullOrWhiteSpace(request.Mobile) ||
                !string.IsNullOrWhiteSpace(request.Email) ||
                !string.IsNullOrWhiteSpace(request.Address))
            {
                var c = merchant.Contact;
                merchant.UpdateContact(new OpenFindBearings.Domain.ValueObjects.ContactInfo(
                    request.ContactPerson ?? c?.ContactPerson,
                    request.Phone ?? c?.Phone,
                    request.Mobile ?? c?.Mobile,
                    request.Email ?? c?.Email,
                    request.Address ?? c?.Address));
            }

            merchant.SubmitForApproval();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 记录被提名人 sub（审核通过时据此建管理员成员行）
            var nomineeUser = await _userRepository.GetByIdAsync(request.NomineeUserId, cancellationToken);
            if (nomineeUser == null)
            {
                throw new InvalidOperationException("登录用户不存在");
            }
            invitation.Complete(nomineeUser.AuthUserId);
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            // 可选提交营业执照
            if (!string.IsNullOrWhiteSpace(request.LicenseUrl))
            {
                await _licenseRepository.AddAsync(
                    new OpenFindBearings.Domain.Entities.LicenseVerification(
                        merchant.Id, request.LicenseUrl, request.NomineeUserId),
                    cancellationToken);
            }

            _logger.LogInformation("管理员提名已接受: MerchantId={MerchantId}, Code={Code}", merchant.Id, request.InvitationCode);
            return merchant.Id;
        }
    }
}

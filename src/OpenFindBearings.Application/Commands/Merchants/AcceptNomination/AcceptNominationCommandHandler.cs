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
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<AcceptNominationCommandHandler> _logger;

        public AcceptNominationCommandHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantRepository merchantRepository,
            ILicenseVerificationRepository licenseRepository,
            IUserRepository userRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<AcceptNominationCommandHandler> logger)
        {
            _invitationRepository = invitationRepository;
            _merchantRepository = merchantRepository;
            _licenseRepository = licenseRepository;
            _userRepository = userRepository;
            _merchantMemberRepository = merchantMemberRepository;
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
            if (merchant == null)
            {
                throw new InvalidOperationException("提名商户不存在");
            }

            // 改动说明：区分两种提名目标——
            //   Draft（提名新建）：接受即补资料并 Draft→Pending；
            //   已有未认证商家（提名认领已有）：接受=预认领，保持其原状态（不 Draft→Pending）。
            //   两种情况的成员行都由审核通过时统一创建（ApproveMerchant.CreateNominationMembersAsync）。
            var isDraftNomination = merchant.Status == MerchantStatus.Draft;
            if (isDraftNomination)
            {
                // 新建提名走 Draft→Pending，无需额外校验归属
            }
            else
            {
                // 已有商家提名：接受时再校验其仍未被认领/未认证（提名发出后状态可能已变动）
                if (merchant.IsVerified)
                {
                    throw new InvalidOperationException("该商家已认证，无法接受提名");
                }
                var activeMembers = await _merchantMemberRepository.GetActiveByMerchantAsync(merchant.Id, cancellationToken);
                if (activeMembers.Count > 0)
                {
                    throw new InvalidOperationException("该商家已被他人认领");
                }
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

            // 改动说明：接受提名=进入真人维护链路，来源置 Manual 使其不再被 Sync 爬虫覆盖
            merchant.SetDataSource(OpenFindBearings.Domain.ValueObjects.DataSource.FromManual(request.NomineeUserId.ToString()));

            // 仅"提名新建"需 Draft→Pending；"提名认领已有商家"保持其原状态（本就非 Draft）
            if (isDraftNomination)
            {
                merchant.SubmitForApproval();
            }
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

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AddStaff
{
    /// <summary>
    /// 添加员工命令处理器
    /// </summary>
    public class AddStaffCommandHandler : IRequestHandler<AddStaffCommand, AddStaffResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IIdentityService _identityService;
        // 改动说明（v2.9.0）：邀请确认制需要给被邀人发站内信
        private readonly INotificationService _notificationService;
        private readonly ILogger<AddStaffCommandHandler> _logger;

        public AddStaffCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IStaffInvitationRepository invitationRepository,
            IIdentityService identityService,
            INotificationService notificationService,
            ILogger<AddStaffCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _invitationRepository = invitationRepository;
            _identityService = identityService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<AddStaffResult> Handle(AddStaffCommand request, CancellationToken cancellationToken)
        {
            var contactInfo = request.GetContactInfo();
            _logger.LogInformation("添加员工: MerchantId={MerchantId}, Contact={Contact}, OperatorId={OperatorId}",
                request.MerchantId, contactInfo, request.OperatorId);

            // 改动说明：由 User.MerchantId 单值校验改为成员表校验操作人是该商户在职管理员
            var operatorMember = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorId, request.MerchantId, cancellationToken);
            if (operatorMember == null || !operatorMember.IsAdmin)
            {
                throw new UnauthorizedAccessException("您无权添加员工");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            OidcUserInfo? oidcUser = null;
            string? foundBy = null;

            if (!string.IsNullOrEmpty(request.Email))
            {
                oidcUser = await _identityService.GetUserByEmailAsync(request.Email, cancellationToken);
                if (oidcUser != null) foundBy = "email";
            }

            if (oidcUser == null && !string.IsNullOrEmpty(request.Phone))
            {
                oidcUser = await _identityService.GetUserByPhoneAsync(request.Phone, cancellationToken);
                if (oidcUser != null) foundBy = "phone";
            }

            if (oidcUser != null)
            {
                return await LinkExistingUserAsync(
                    request,
                    oidcUser,
                    foundBy!,
                    cancellationToken);
            }

            return await SendInvitationAsync(request, cancellationToken);
        }

        private async Task<AddStaffResult> LinkExistingUserAsync(
            AddStaffCommand request,
            OidcUserInfo oidcUser,
            string foundBy,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户已存在，通过 {FoundBy} 找到: Sub={Sub}",
                foundBy, oidcUser.Sub);

            var user = await _userRepository.GetByAuthUserIdAsync(oidcUser.Sub, cancellationToken);
            if (user == null)
            {
                var nickname = oidcUser.GetDisplayName();
                user = new User(
                    authUserId: oidcUser.Sub,
                    registrationSource: RegistrationSource.Admin,
                    registerIp: null,
                    nickname: nickname);
                await _userRepository.AddAsync(user, cancellationToken);
            }

            // 改动说明（v2.9.0 邀请确认制）：已注册用户不再"静默拉入"（被邀人无感知、
            //   列表却看不到成员，与钉钉/飞书邀请需本人同意的主流语义不符）。
            //   改为建 Type=Staff 待确认邀请 + 站内信通知对方，对方在商户页同意后入伙。
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            var merchantName = merchant?.Name ?? "商家";
            var operatorUser = await _userRepository.GetByIdAsync(request.OperatorId, cancellationToken);
            var operatorName = operatorUser?.Nickname ?? "商户管理员";

            // 已有在职成员或已有待确认邀请 → 幂等提示，不重复发
            var existingMember = await _merchantMemberRepository.GetByUserAndMerchantAsync(user.Id, request.MerchantId, cancellationToken);
            if (existingMember != null && existingMember.Status == MerchantMemberStatus.Active)
            {
                return AddStaffResult.AlreadyMember(merchantName);
            }

            var targetRole = request.Role ?? MerchantMember.RoleMerchantStaff;
            var invitationCode = Guid.NewGuid().ToString("N")[..12];
            var invitation = new StaffInvitation(
                request.MerchantId,
                request.Email,
                // 改动说明：邀请 Phone 优先取 Identity 档案手机号（被邀人 JWT claim 匹配依据），
                //   再退请求手机号——管理员按邮箱添加时 request.Phone 为空，若记 null 被邀人永远匹配不到邀请
                oidcUser.PhoneNumber ?? request.Phone,
                targetRole,
                invitationCode,
                request.OperatorId,
                type: InvitationType.Staff);
            await _invitationRepository.AddAsync(invitation, cancellationToken);

            await _notificationService.AddInAppAsync(
                user.Id,
                Notification.TypeStaffJoinInvited,
                "商户邀请待确认",
                $"{operatorName} 邀请你加入商户「{merchantName}」，请前往“我的-商家”页面查看并接受或拒绝。",
                Notification.BizMerchant,
                request.MerchantId,
                cancellationToken);

            _logger.LogInformation("员工邀请已发送（待确认）: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);

            return AddStaffResult.InvitationSent(invitation.Id, emailSent: false, smsSent: false);
        }

        // 改动说明（v2.9.0）：EnsureMemberAsync 已移除——直接入伙路径被邀请确认制取代，
        //   成员行创建逻辑移至 AcceptStaffInvitationCommand（被邀人同意时才建）

        private async Task<AddStaffResult> SendInvitationAsync(AddStaffCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户不存在，发送邀请: Email={Email}, Phone={Phone}",
                request.Email, request.Phone);

            var invitationCode = Guid.NewGuid().ToString("N")[..12];
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            var merchantName = merchant?.Name ?? "商家";

            var invitationId = await _identityService.RecordInvitationAsync(
                merchantId: request.MerchantId,
                email: request.Email,
                phone: request.Phone,
                role: request.Role,
                invitationCode: invitationCode,
                operatorId: request.OperatorId,
                cancellationToken);

            bool emailSent = false;
            bool smsSent = false;

            if (!string.IsNullOrEmpty(request.Email))
            {
                await _identityService.SendEmailInvitationAsync(
                    email: request.Email,
                    merchantName: merchantName,
                    invitationCode: invitationCode,
                    cancellationToken);
                emailSent = true;
                _logger.LogInformation("邮件邀请已发送: {Email}", request.Email);
            }

            if (!string.IsNullOrEmpty(request.Phone))
            {
                await _identityService.SendSmsInvitationAsync(
                    phone: request.Phone,
                    merchantName: merchantName,
                    invitationCode: invitationCode,
                    cancellationToken);
                smsSent = true;
                _logger.LogInformation("短信邀请已发送: {Phone}", request.Phone);
            }

            return AddStaffResult.InvitationSent(invitationId, emailSent, smsSent);
        }
    }
}

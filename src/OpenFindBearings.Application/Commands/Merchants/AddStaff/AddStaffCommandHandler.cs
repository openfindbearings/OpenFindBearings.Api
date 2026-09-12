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
        private readonly ILogger<AddStaffCommandHandler> _logger;

        public AddStaffCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IStaffInvitationRepository invitationRepository,
            IIdentityService identityService,
            ILogger<AddStaffCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _invitationRepository = invitationRepository;
            _identityService = identityService;
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

            // 改动说明：一人多商户，同一用户可属于多个商户，取消"已是其他商家的员工"硬约束；
            //           商户域角色写入成员表，取代全局角色分配
            var targetRole = request.Role ?? MerchantMember.RoleMerchantStaff;
            await EnsureMemberAsync(user, request.MerchantId, targetRole, request.OperatorId, cancellationToken);

            _logger.LogInformation("员工添加成功: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);

            return AddStaffResult.Linked(user.Id);
        }

        /// <summary>
        /// 确保用户成为该商户在职成员（复用 Removed 行，不新增第二行）
        /// </summary>
        private async Task EnsureMemberAsync(
            User user,
            Guid merchantId,
            string role,
            Guid invitedBy,
            CancellationToken cancellationToken)
        {
            var existingMember = await _merchantMemberRepository.GetByUserAndMerchantAsync(user.Id, merchantId, cancellationToken);
            if (existingMember == null)
            {
                var member = new MerchantMember(user.Id, merchantId, role, invitedBy);
                await _merchantMemberRepository.AddAsync(member, cancellationToken);
            }
            else
            {
                existingMember.Rejoin(role, invitedBy);
                await _merchantMemberRepository.UpdateAsync(existingMember, cancellationToken);
            }

            // 兼容遗留读取：User.MerchantId 与首个成员保持一致（成员表才是事实源）
            if (user.MerchantId != merchantId)
            {
                user.AssignToMerchant(merchantId);
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }

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

using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 添加员工命令处理器
    /// </summary>
    public class AddStaffCommandHandler : IRequestHandler<AddStaffCommand, AddStaffResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IIdentityService _identityService;
        private readonly ILogger<AddStaffCommandHandler> _logger;

        public AddStaffCommandHandler(
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IStaffInvitationRepository invitationRepository,
            IIdentityService identityService,
            ILogger<AddStaffCommandHandler> logger)
        {
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _invitationRepository = invitationRepository;
            _identityService = identityService;
            _logger = logger;
        }

        public async Task<AddStaffResult> Handle(AddStaffCommand request, CancellationToken cancellationToken)
        {
            var contactInfo = request.GetContactInfo();
            _logger.LogInformation("添加员工: MerchantId={MerchantId}, Contact={Contact}, OperatorId={OperatorId}",
                request.MerchantId, contactInfo, request.OperatorId);

            // 1. 检查操作人权限
            var operatorUser = await _userRepository.GetByIdAsync(request.OperatorId, cancellationToken);
            if (operatorUser == null || operatorUser.MerchantId != request.MerchantId)
            {
                throw new UnauthorizedAccessException("您无权添加员工");
            }

            // 2. 检查商家是否存在
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 3. 尝试通过邮箱或手机号查找用户
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

            // 4. 如果用户存在，直接关联
            if (oidcUser != null)
            {
                return await LinkExistingUserAsync(
                    request,
                    oidcUser,
                    foundBy!,
                    cancellationToken);
            }

            // 5. 用户不存在，发送邀请
            return await SendInvitationAsync(request, cancellationToken);
        }

        /// <summary>
        /// 关联已存在的用户
        /// </summary>
        private async Task<AddStaffResult> LinkExistingUserAsync(
            AddStaffCommand request,
            OidcUserInfo oidcUser,
            string foundBy,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户已存在，通过 {FoundBy} 找到: Sub={Sub}",
                foundBy, oidcUser.Sub);

            // 根据 Sub (AuthUserId) 获取或创建业务用户
            var user = await _userRepository.GetByAuthUserIdAsync(oidcUser.Sub, cancellationToken);
            if (user == null)
            {
                var nickname = oidcUser.GetDisplayName();
                user = new User(oidcUser.Sub, UserType.MerchantStaff, nickname);
                await _userRepository.AddAsync(user, cancellationToken);
            }

            // 如果用户已经是其他商家的员工，不能重复添加
            if (user.MerchantId.HasValue && user.MerchantId != request.MerchantId)
            {
                throw new InvalidOperationException("该用户已是其他商家的员工");
            }

            // 关联用户到商家
            if (!user.MerchantId.HasValue)
            {
                user.AssignToMerchant(request.MerchantId);
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            // 分配角色
            await AssignRoleAsync(user, request.Role, cancellationToken);

            _logger.LogInformation("员工添加成功: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);

            return AddStaffResult.Linked(user.Id);
        }

        /// <summary>
        /// 发送邀请
        /// </summary>
        private async Task<AddStaffResult> SendInvitationAsync(AddStaffCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("用户不存在，发送邀请: Email={Email}, Phone={Phone}",
                request.Email, request.Phone);

            // 生成邀请码
            var invitationCode = Guid.NewGuid().ToString("N")[..12];

            // 获取商家名称
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            var merchantName = merchant?.Name ?? "商家";

            // 记录邀请信息
            var invitationId = await _identityService.RecordInvitationAsync(
                merchantId: request.MerchantId,
                email: request.Email,
                phone: request.Phone,
                role: request.Role,
                operatorId: request.OperatorId,
                cancellationToken);

            bool emailSent = false;
            bool smsSent = false;

            // 发送邮件邀请
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

            // 发送短信邀请
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

        /// <summary>
        /// 分配角色
        /// </summary>
        private async Task AssignRoleAsync(User user, string? roleName, CancellationToken cancellationToken)
        {
            var targetRole = roleName ?? "MerchantStaff";
            var role = await _roleRepository.GetByNameAsync(targetRole, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("角色不存在: {RoleName}", targetRole);
                return;
            }

            var hasRole = await _userRoleRepository.UserHasRoleAsync(user.Id, role.Name, cancellationToken);
            if (!hasRole)
            {
                await _userRoleRepository.AddUserToRoleAsync(user.Id, role.Id, cancellationToken);
                _logger.LogDebug("角色已分配: UserId={UserId}, Role={RoleName}", user.Id, targetRole);
            }
        }
    }
}

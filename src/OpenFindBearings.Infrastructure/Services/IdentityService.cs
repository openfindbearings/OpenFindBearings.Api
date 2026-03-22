using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using System.Text.Json;

namespace OpenFindBearings.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<IdentityService> _logger;
        private readonly IdentityServiceOptions _options;

        public IdentityService(
            HttpClient httpClient,
            IStaffInvitationRepository invitationRepository,
            IOptions<IdentityServiceOptions> options,
            ILogger<IdentityService> logger)
        {
            _httpClient = httpClient;
            _invitationRepository = invitationRepository;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<OidcUserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/by-email?email={Uri.EscapeDataString(email)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Email={Email}", email);
                return null;
            }
        }

        public async Task<OidcUserInfo?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/by-phone?phone={Uri.EscapeDataString(phone)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Phone={Phone}", phone);
                return null;
            }
        }

        public async Task<OidcUserInfo?> GetUserByIdAsync(string sub, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/{sub}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Sub={Sub}", sub);
                return null;
            }
        }

        public async Task<Guid> RecordInvitationAsync(
            Guid merchantId,
            string? email,
            string? phone,
            string? role,
            Guid operatorId,
            CancellationToken cancellationToken)
        {
            var invitation = new StaffInvitation(
                merchantId,
                email,
                phone,
                role,
                Guid.NewGuid().ToString("N")[..12],
                operatorId);

            await _invitationRepository.AddAsync(invitation, cancellationToken);
            return invitation.Id;
        }

        public async Task SendEmailInvitationAsync(
            string email,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken)
        {
            // TODO: 调用邮件服务
            var inviteUrl = $"{_options.WebAppUrl}/register?code={invitationCode}";
            _logger.LogInformation("发送邮件邀请: {Email}, 邀请链接: {InviteUrl}", email, inviteUrl);
            await Task.CompletedTask;
        }

        public async Task SendSmsInvitationAsync(
            string phone,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken)
        {
            // TODO: 调用短信服务
            var inviteUrl = $"{_options.WebAppUrl}/register?code={invitationCode}";
            _logger.LogInformation("发送短信邀请: {Phone}, 邀请链接: {InviteUrl}", phone, inviteUrl);
            await Task.CompletedTask;
        }

        public async Task<InvitationResult> CompleteInvitationAsync(
            string invitationCode,
            string sub,
            CancellationToken cancellationToken)
        {
            var invitation = await _invitationRepository.GetByCodeAsync(invitationCode, cancellationToken);
            if (invitation == null)
            {
                return InvitationResult.Failed("邀请码无效");
            }

            if (invitation.IsCompleted)
            {
                return InvitationResult.Failed("邀请已使用");
            }

            if (invitation.IsExpired())
            {
                return InvitationResult.Failed("邀请已过期");
            }

            invitation.Complete(sub);
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            return InvitationResult.Success(invitation.MerchantId);
        }
    }

    public class IdentityServiceOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string WebAppUrl { get; set; } = string.Empty;
    }
}

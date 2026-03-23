using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using System.Text.Json;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 认证服务客户端实现
    /// 负责与 OpenIddict 认证服务通信
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly ILogger<IdentityService> _logger;
        private readonly IdentityServiceOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

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

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region 用户查询

        public async Task<OidcUserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/by-email?email={Uri.EscapeDataString(email)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Email={Email}", email);
                return null;
            }
        }

        public async Task<OidcUserInfo?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/by-phone?phone={Uri.EscapeDataString(phone)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Phone={Phone}", phone);
                return null;
            }
        }

        public async Task<OidcUserInfo?> GetUserByIdAsync(string sub, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_options.BaseUrl}/api/users/{sub}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<OidcUserInfo>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取认证用户失败: Sub={Sub}", sub);
                return null;
            }
        }

        #endregion

        #region 邀请管理

        public async Task<Guid> RecordInvitationAsync(
            Guid merchantId,
            string? email,
            string? phone,
            string? role,
            Guid operatorId,
            CancellationToken cancellationToken = default)
        {
            var invitation = new StaffInvitation(
                merchantId, email, phone, role,
                Guid.NewGuid().ToString("N")[..12],
                operatorId);

            await _invitationRepository.AddAsync(invitation, cancellationToken);
            return invitation.Id;
        }

        public async Task SendEmailInvitationAsync(
            string email,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken = default)
        {
            var inviteUrl = $"{_options.WebAppUrl}/register?code={invitationCode}&type=staff";
            _logger.LogInformation("发送邮件邀请: {Email}, 邀请链接: {InviteUrl}", email, inviteUrl);
            await Task.CompletedTask;
        }

        public async Task SendSmsInvitationAsync(
            string phone,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken = default)
        {
            var inviteUrl = $"{_options.WebAppUrl}/register?code={invitationCode}&type=staff";
            _logger.LogInformation("发送短信邀请: {Phone}, 邀请链接: {InviteUrl}", phone, inviteUrl);
            await Task.CompletedTask;
        }

        public async Task<InvitationResult> CompleteInvitationAsync(
            string invitationCode,
            string sub,
            CancellationToken cancellationToken = default)
        {
            var invitation = await _invitationRepository.GetByCodeAsync(invitationCode, cancellationToken);

            if (invitation == null)
                return InvitationResult.Failed("邀请码无效");
            if (invitation.IsCompleted)
                return InvitationResult.Failed("邀请已使用");
            if (invitation.IsExpired())
                return InvitationResult.Failed("邀请已过期");

            invitation.Complete(sub);
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            return InvitationResult.Success(invitation.MerchantId);
        }

        #endregion

        #region 账户管理

        public async Task<IdentityRegisterResult> RegisterAsync(RegisterIdentityRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_options.BaseUrl}/api/users/register",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    return IdentityRegisterResult.Failed(error ?? "注册失败");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<RegisterResponse>(json, _jsonOptions);

                return IdentityRegisterResult.Succeeded(result?.UserId ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册失败: Email={Email}", request.Email);
                return IdentityRegisterResult.Failed("注册失败，请稍后重试");
            }
        }

        public async Task<bool> SendPhoneVerificationCodeAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { phoneNumber };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_options.BaseUrl}/api/sms/send",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送短信验证码失败: Phone={Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetCodeAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { email };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_options.BaseUrl}/api/users/forgot-password",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送重置密码验证码失败: Email={Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { email, code, newPassword };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_options.BaseUrl}/api/users/reset-password",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败: Email={Email}", email);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string authUserId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { currentPassword, newPassword };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_options.BaseUrl}/api/users/{authUserId}/password",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码失败: AuthUserId={AuthUserId}", authUserId);
                return false;
            }
        }

        public async Task<bool> UpdatePhoneAsync(string authUserId, string phoneNumber, string verificationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { phoneNumber, verificationCode };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_options.BaseUrl}/api/users/{authUserId}/phone",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新手机号失败: AuthUserId={AuthUserId}", authUserId);
                return false;
            }
        }

        public async Task<bool> UpdateEmailAsync(string authUserId, string email, string verificationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new { email, verificationCode };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_options.BaseUrl}/api/users/{authUserId}/email",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新邮箱失败: AuthUserId={AuthUserId}", authUserId);
                return false;
            }
        }

        #endregion
    }

    public class IdentityServiceOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string WebAppUrl { get; set; } = string.Empty;
    }

    internal class RegisterResponse
    {
        public string UserId { get; set; } = string.Empty;
    }
}

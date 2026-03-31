using OpenFindBearings.Application.Common.Models;

namespace OpenFindBearings.Application.Interfaces
{
    /// <summary>
    /// 认证服务客户端接口
    /// 用于与 OpenIddict 认证服务通信
    /// </summary>
    public interface IIdentityService
    {
        #region 用户查询

        /// <summary>
        /// 根据邮箱获取认证用户信息
        /// </summary>
        Task<OidcUserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据手机号获取认证用户信息
        /// </summary>
        Task<OidcUserInfo?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据认证用户ID获取用户信息
        /// </summary>
        Task<OidcUserInfo?> GetUserByIdAsync(string sub, CancellationToken cancellationToken = default);

        #endregion

        #region 邀请管理

        /// <summary>
        /// 记录邀请信息
        /// </summary>
        Task<Guid> RecordInvitationAsync(
            Guid merchantId,
            string? email,
            string? phone,
            string? role,
            Guid operatorId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送邮件邀请
        /// </summary>
        Task SendEmailInvitationAsync(
            string email,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送短信邀请
        /// </summary>
        Task SendSmsInvitationAsync(
            string phone,
            string merchantName,
            string invitationCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户通过邀请链接完成注册后调用
        /// </summary>
        Task<InvitationResult> CompleteInvitationAsync(
            string invitationCode,
            string sub,
            CancellationToken cancellationToken = default);

        #endregion

        #region 账户管理

        /// <summary>
        /// 注册新用户
        /// </summary>
        Task<IdentityRegisterResult> RegisterAsync(RegisterIdentityRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送短信验证码
        /// </summary>
        Task<bool> SendPhoneVerificationCodeAsync(string phoneNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送密码重置验证码
        /// </summary>
        Task<bool> SendPasswordResetCodeAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 修改密码
        /// </summary>
        Task<bool> ChangePasswordAsync(string authUserId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新手机号
        /// </summary>
        Task<bool> UpdatePhoneAsync(string authUserId, string phoneNumber, string verificationCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新邮箱
        /// </summary>
        Task<bool> UpdateEmailAsync(string authUserId, string email, string verificationCode, CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// 邀请完成结果
    /// </summary>
    public class InvitationResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? UserId { get; set; }

        public static InvitationResult Success(Guid merchantId) =>
            new() { IsSuccess = true, MerchantId = merchantId };

        public static InvitationResult Failed(string message) =>
            new() { IsSuccess = false, Message = message };
    }

    /// <summary>
    /// 注册请求
    /// </summary>
    public class RegisterIdentityRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 注册结果
    /// </summary>
    public class IdentityRegisterResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }

        public static IdentityRegisterResult Succeeded(string userId) =>
            new() { Success = true, UserId = userId };

        public static IdentityRegisterResult Failed(string message) =>
            new() { Success = false, Message = message };
    }
}

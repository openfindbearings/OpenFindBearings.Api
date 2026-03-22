using OpenFindBearings.Application.Common.Models;

namespace OpenFindBearings.Application.Common.Interfaces
{
    /// <summary>
    /// 认证服务客户端接口
    /// </summary>
    public interface IIdentityService
    {
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
}

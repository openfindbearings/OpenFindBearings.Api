using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 从商家移除用户命令
    /// </summary>
    public record RemoveUserFromMerchantCommand : IRequest
    {
        /// <summary>
        /// 用户认证ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }
    }
}

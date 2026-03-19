using MediatR;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 分配用户到商家命令
    /// </summary>
    public record AssignUserToMerchantCommand : IRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 在商家的角色（可选）
        /// </summary>
        public string? MerchantRole { get; init; }
    }
}

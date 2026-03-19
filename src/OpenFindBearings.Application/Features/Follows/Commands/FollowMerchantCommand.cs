using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Commands
{
    /// <summary>
    /// 关注商家命令
    /// </summary>
    public record FollowMerchantCommand : IRequest<bool>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }
    }
}

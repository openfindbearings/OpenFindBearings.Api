using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Follows.FollowMerchant
{
    /// <summary>
    /// 关注商家命令
    /// </summary>
    public record FollowMerchantCommand : IRequest<bool>, ICommand
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

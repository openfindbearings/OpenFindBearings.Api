using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Follows.UnfollowMerchant
{
    /// <summary>
    /// 取消关注商家命令
    /// </summary>
    public record UnfollowMerchantCommand : IRequest, ICommand
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

using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Commands
{
    /// <summary>
    /// 取消关注商家命令
    /// </summary>
    public record UnfollowMerchantCommand : IRequest
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

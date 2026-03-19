using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Queries
{
    /// <summary>
    /// 检查商家是否已关注查询
    /// </summary>
    public record CheckMerchantFollowQuery : IRequest<bool>
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

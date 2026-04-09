using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Follows.CheckMerchantFollow
{
    /// <summary>
    /// 检查商家是否已关注查询
    /// </summary>
    public record CheckMerchantFollowQuery : IRequest<bool>, IQuery
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

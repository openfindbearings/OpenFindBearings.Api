using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Queries
{
    /// <summary>
    /// 检查商家是否已关注查询
    /// </summary>
    public record CheckMerchantFollowQuery(
        Guid MerchantId,
        string AuthUserId
    ) : IRequest<bool>;
}

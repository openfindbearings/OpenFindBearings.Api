using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 检查商家-轴承关联是否存在查询
    /// </summary>
    public record CheckMerchantBearingExistsQuery(
        Guid MerchantId,
        Guid BearingId
    ) : IRequest<bool>;
}

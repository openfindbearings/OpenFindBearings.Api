using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取单个商家-轴承关联查询
    /// </summary>
    public record GetMerchantBearingQuery(Guid Id) : IRequest<MerchantBearingDto?>;
}

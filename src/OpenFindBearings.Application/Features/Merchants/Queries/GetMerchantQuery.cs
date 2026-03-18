using MediatR;
using OpenFindBearings.Application.Features.Merchants.DTOs;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 获取单个商家查询
    /// </summary>
    public record GetMerchantQuery(Guid Id) : IRequest<MerchantDetailDto?>;
}

using MediatR;
using OpenFindBearings.Application.Features.Merchants.DTOs;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 根据用户ID获取商家查询
    /// </summary>
    public record GetMerchantByUserIdQuery : IRequest<MerchantDetailDto?>
    {
        public Guid UserId { get; init; }
    }
}

using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantByUserId
{
    /// <summary>
    /// 根据用户ID获取商家查询
    /// </summary>
    public record GetMerchantByUserIdQuery : IRequest<MerchantDetailDto?>, IQuery
    {
        public Guid UserId { get; init; }
    }
}

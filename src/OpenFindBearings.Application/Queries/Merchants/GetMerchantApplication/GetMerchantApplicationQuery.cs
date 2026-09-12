using MediatR;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantApplication
{
    /// <summary>
    /// 查询用户在各商户的入驻状态查询
    /// </summary>
    public record GetMerchantApplicationQuery(Guid UserId) : IRequest<List<MerchantApplicationDto>>;
}

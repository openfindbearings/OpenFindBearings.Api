using MediatR;
using OpenFindBearings.Application.Features.Merchants.DTOs;

namespace OpenFindBearings.Application.Features.Merchants.Queries
{
    /// <summary>
    /// 获取商家员工列表查询
    /// </summary>
    public record GetMerchantStaffQuery(Guid MerchantId) : IRequest<List<MerchantStaffDto>>;
}

using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantStaff
{
    /// <summary>
    /// 获取商家员工列表查询
    /// </summary>
    public record GetMerchantStaffQuery(Guid MerchantId) : IRequest<List<MerchantStaffDto>>, IQuery;
}

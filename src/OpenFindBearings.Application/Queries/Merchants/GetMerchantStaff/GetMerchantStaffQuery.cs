using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchantStaff
{
    /// <summary>
    /// 获取商家员工列表查询
    /// 改动说明（v1.5.2）：加 CurrentUserId 用于标记 isSelf——Taro 登录态 id 是 Identity sub，
    ///   与成员表 UserId（API 业务库主键）不同源，前端无法自行判定"是不是我"，由后端权威标记
    /// </summary>
    public record GetMerchantStaffQuery(Guid MerchantId, Guid? CurrentUserId = null) : IRequest<List<MerchantStaffDto>>, IQuery;
}

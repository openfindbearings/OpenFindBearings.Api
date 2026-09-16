using MediatR;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetApplicationDetail
{
    /// <summary>
    /// 查询单个入驻申请详情（v2.6.0 新增，被拒后"修改并重新提交"表单预填用）。
    /// 处理器内含守卫：调用者必须是该商户在职成员，越权返回 null。
    /// </summary>
    public record GetApplicationDetailQuery(Guid UserId, Guid MerchantId) : IRequest<MerchantApplicationDetailDto?>;
}

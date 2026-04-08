using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Merchants.GetMerchant
{
    /// <summary>
    /// 获取单个商家查询
    /// </summary>
    public record GetMerchantQuery : IRequest<MerchantDetailDto?>, IQuery
    {
        public Guid Id { get; init; }

        /// <summary>
        /// 当前用户是否已登录（由API层传入）
        /// </summary>
        public bool IsAuthenticated { get; init; }
    }
}

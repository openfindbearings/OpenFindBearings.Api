using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取单个商家-轴承关联查询
    /// </summary>
    public record GetMerchantBearingQuery : IRequest<MerchantBearingDto?>
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 当前用户是否已登录（由API层传入）
        /// </summary>
        public bool IsAuthenticated { get; init; }
    }
}

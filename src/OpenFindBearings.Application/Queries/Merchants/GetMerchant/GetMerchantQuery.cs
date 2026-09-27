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

        /// <summary>
        /// v2.6.0 商家主页：当前用户 ID（可空=未登录；成员标记与角色判定输入）
        /// </summary>
        public Guid? UserId { get; init; }
    }
}

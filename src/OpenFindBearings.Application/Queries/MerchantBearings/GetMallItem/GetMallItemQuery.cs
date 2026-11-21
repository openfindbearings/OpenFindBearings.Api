using MediatR;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMallItem
{
    /// <summary>
    /// 商城商品详情查询（v1.59.16）：按商品 Id 取商家 x 商品维度的单品详情。
    /// 匿名可访问；不可见（下架/未认证/停用）返回 null，由端点转 404。
    /// </summary>
    public record GetMallItemQuery(Guid Id) : IRequest<MallItemDetailDto?>;
}
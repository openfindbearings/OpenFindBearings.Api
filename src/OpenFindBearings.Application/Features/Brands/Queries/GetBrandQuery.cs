using MediatR;
using OpenFindBearings.Application.Features.Brands.DTOs;

namespace OpenFindBearings.Application.Features.Brands.Queries
{
    /// <summary>
    /// 获取单个品牌查询
    /// </summary>
    public record GetBrandQuery(Guid Id) : IRequest<BrandDto?>;
}

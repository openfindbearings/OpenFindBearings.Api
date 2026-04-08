using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Brands.GetBrand
{
    /// <summary>
    /// 获取单个品牌查询
    /// </summary>
    public record GetBrandQuery(Guid Id) : IRequest<BrandDto?>, IQuery;
}

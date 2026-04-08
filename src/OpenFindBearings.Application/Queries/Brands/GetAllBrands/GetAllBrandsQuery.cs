using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Brands.GetAllBrands
{
    /// <summary>
    /// 获取所有品牌列表查询
    /// </summary>
    public record GetAllBrandsQuery : IRequest<List<BrandDto>>, IQuery;
}

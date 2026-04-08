using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Brands
{
    /// <summary>
    /// 根据代码获取品牌查询
    /// </summary>
    public record GetBrandByCodeQuery(string Code) : IRequest<BrandDto?>, IQuery;
}

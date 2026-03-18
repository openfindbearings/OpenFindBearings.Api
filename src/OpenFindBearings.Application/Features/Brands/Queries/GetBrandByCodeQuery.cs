using MediatR;
using OpenFindBearings.Application.Features.Brands.DTOs;

namespace OpenFindBearings.Application.Features.Brands.Queries
{
    /// <summary>
    /// 根据代码获取品牌查询
    /// </summary>
    public record GetBrandByCodeQuery(string Code) : IRequest<BrandDto?>;
}

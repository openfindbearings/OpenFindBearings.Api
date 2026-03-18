using MediatR;
using OpenFindBearings.Application.Features.Brands.DTOs;

namespace OpenFindBearings.Application.Features.Brands.Queries
{
    /// <summary>
    /// 获取所有品牌列表查询
    /// </summary>
    public record GetAllBrandsQuery : IRequest<List<BrandDto>>;
}

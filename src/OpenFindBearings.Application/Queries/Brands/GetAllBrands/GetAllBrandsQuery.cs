using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Brands.GetAllBrands
{
    /// <summary>
    /// 获取所有品牌列表查询
    /// </summary>
    public record GetAllBrandsQuery : IRequest<List<BrandDto>>, IQuery
    {
        /// <summary>
        /// 是否包含已删除的品牌（null=仅激活，true=全部）
        /// </summary>
        public bool? IncludeDeleted { get; set; }
    }
}

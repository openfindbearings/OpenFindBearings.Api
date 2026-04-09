using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Mobile.GetMobileBearingLightList
{
    /// <summary>
    /// 获取移动端轴承轻量列表查询
    /// </summary>
    public record GetMobileBearingLightListQuery : IRequest<PagedResult<MobileBearingLightDto>>, IQuery
    {
        public string? Keyword { get; set; }
        public decimal? MinInnerDiameter { get; set; }
        public decimal? MaxInnerDiameter { get; set; }
        public Guid? BrandId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

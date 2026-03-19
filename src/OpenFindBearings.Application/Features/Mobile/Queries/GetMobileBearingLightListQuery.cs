using MediatR;
using OpenFindBearings.Application.Features.Mobile.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.Mobile.Queries
{
    /// <summary>
    /// 获取移动端轴承轻量列表查询
    /// </summary>
    public class GetMobileBearingLightListQuery : IRequest<PagedResult<MobileBearingLightDto>>
    {
        public string? Keyword { get; set; }
        public decimal? MinInnerDiameter { get; set; }
        public decimal? MaxInnerDiameter { get; set; }
        public Guid? BrandId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

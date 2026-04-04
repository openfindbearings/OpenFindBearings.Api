using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 搜索轴承查询
    /// </summary>
    public class SearchBearingsQuery : IRequest<PagedResult<BearingDto>>
    {
        /// <summary>
        /// 现行代号（模糊搜索）
        /// </summary>
        public string? CurrentCode { get; set; }

        /// <summary>
        /// 曾用代号（模糊搜索）
        /// </summary>
        public string? FormerCode { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 最小内径
        /// </summary>
        public decimal? MinInnerDiameter { get; set; }

        /// <summary>
        /// 最大内径
        /// </summary>
        public decimal? MaxInnerDiameter { get; set; }

        /// <summary>
        /// 最小外径
        /// </summary>
        public decimal? MinOuterDiameter { get; set; }

        /// <summary>
        /// 最大外径
        /// </summary>
        public decimal? MaxOuterDiameter { get; set; }

        /// <summary>
        /// 最小宽度
        /// </summary>
        public decimal? MinWidth { get; set; }

        /// <summary>
        /// 最大宽度
        /// </summary>
        public decimal? MaxWidth { get; set; }

        /// <summary>
        /// 产地筛选
        /// </summary>
        public string? OriginCountry { get; set; }

        /// <summary>
        /// 产品类别筛选
        /// </summary>
        public BearingCategory? Category { get; set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// 轴承类型ID
        /// </summary>
        public Guid? BearingTypeId { get; set; }

        /// <summary>
        /// 是否标准轴承
        /// </summary>
        public bool? IsStandard { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// 排序方向
        /// </summary>
        public string? SortOrder { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

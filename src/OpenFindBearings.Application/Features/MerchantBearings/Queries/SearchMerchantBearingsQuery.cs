using MediatR;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 搜索商家-轴承关联查询
    /// </summary>
    public class SearchMerchantBearingsQuery : IRequest<PagedResult<MerchantBearingDto>>
    {
        /// <summary>
        /// 商家ID（筛选指定商家的产品）
        /// </summary>
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 轴承ID（筛选指定轴承的商家）
        /// </summary>
        public Guid? BearingId { get; set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// 是否只显示在售商品
        /// </summary>
        public bool? IsOnSale { get; set; }

        /// <summary>
        /// 是否只显示推荐商品
        /// </summary>
        public bool? IsFeatured { get; set; }

        /// <summary>
        /// 关键词（搜索商家名称、轴承型号）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 排序字段（price, stock, created等）
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// 排序方向
        /// </summary>
        public string? SortOrder { get; set; } = "asc";
    }
}

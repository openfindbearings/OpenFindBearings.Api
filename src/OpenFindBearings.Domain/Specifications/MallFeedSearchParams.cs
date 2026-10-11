using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Specifications
{
    /// <summary>
    /// 商城货源流搜索参数（v2.14.0 商城导购）：以"商家在售关联"为行的商品流过滤条件。
    /// 与 BearingSearchParams 的区别——这里搜的是 MerchantBearing（某商家在某轴承上的在售条目），
    /// 支持"全局在售"（不指定商家/轴承时为全站在售货源），供商城 Tab 商品瀑布流使用。
    /// </summary>
    public class MallFeedSearchParams
    {
        /// <summary>
        /// 关键词（模糊匹配 轴承现行代号/曾用代号/描述/类型名/品牌名）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// 轴承类型ID（关联字典表）
        /// </summary>
        public Guid? BearingTypeId { get; set; }

        /// <summary>
        /// 产品类别（进口/国产/合资）
        /// </summary>
        public BearingCategory? Category { get; set; }

        /// <summary>
        /// 最小内径 (mm)
        /// </summary>
        public decimal? MinInnerDiameter { get; set; }

        /// <summary>
        /// 最大内径 (mm)
        /// </summary>
        public decimal? MaxInnerDiameter { get; set; }

        /// <summary>
        /// 最小外径 (mm)
        /// </summary>
        public decimal? MinOuterDiameter { get; set; }

        /// <summary>
        /// 最大外径 (mm)
        /// </summary>
        public decimal? MaxOuterDiameter { get; set; }

        /// <summary>
        /// 最小宽度 (mm)
        /// </summary>
        public decimal? MinWidth { get; set; }

        /// <summary>
        /// 最大宽度 (mm)
        /// </summary>
        public decimal? MaxWidth { get; set; }

        /// <summary>
        /// 是否仅在售（商城货源流固定 true；null 保留给管理用途）
        /// </summary>
        public bool? IsOnSale { get; set; } = true;

        /// <summary>
        /// 排序字段（price, created, viewcount；缺省=创建时间倒序）
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// 排序方向（asc, desc）
        /// </summary>
        public string? SortOrder { get; set; } = "desc";

        /// <summary>
        /// 是否已登录（透传给 DTO 决定敏感字段可见性）
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// 页码，从1开始
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数，默认20，最大100
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
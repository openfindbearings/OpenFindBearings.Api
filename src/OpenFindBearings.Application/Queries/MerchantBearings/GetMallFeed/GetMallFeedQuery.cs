using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMallFeed
{
    /// <summary>
    /// 商城货源流查询（v2.14.0 商城导购）：全站在售货源分页，供商城 Tab 商品瀑布流。
    /// 支持关键词、品牌/类型/尺寸/类别过滤与排序；匿名可访问（图片/型号/品牌/商家公开，
    /// 电话等敏感字段按 IsAuthenticated 控制）。
    /// </summary>
    public class GetMallFeedQuery : IRequest<PagedResult<MerchantBearingDto>>, IQuery
    {
        /// <summary>关键词（型号/旧代号/描述/类型名/品牌名）</summary>
        public string? Keyword { get; set; }

        /// <summary>品牌ID</summary>
        public Guid? BrandId { get; set; }

        /// <summary>轴承类型ID</summary>
        public Guid? BearingTypeId { get; set; }

        /// <summary>产品类别（进口/国产/合资）</summary>
        public BearingCategory? Category { get; set; }

        /// <summary>最小内径 (mm)</summary>
        public decimal? MinInnerDiameter { get; set; }

        /// <summary>最大内径 (mm)</summary>
        public decimal? MaxInnerDiameter { get; set; }

        /// <summary>最小外径 (mm)</summary>
        public decimal? MinOuterDiameter { get; set; }

        /// <summary>最大外径 (mm)</summary>
        public decimal? MaxOuterDiameter { get; set; }

        /// <summary>最小宽度 (mm)</summary>
        public decimal? MinWidth { get; set; }

        /// <summary>最大宽度 (mm)</summary>
        public decimal? MaxWidth { get; set; }

        /// <summary>排序字段（price, viewcount, created）</summary>
        public string? SortBy { get; set; }

        /// <summary>排序方向（asc, desc）</summary>
        public string? SortOrder { get; set; } = "desc";

        /// <summary>
        /// 当前用户是否已登录（由端点按登录态覆盖传入，客户端传入值无效）
        /// 改动说明：声明为非空 bool 时，[AsParameters] 会把它当作必填查询参数，
        ///   而 BFF 从不带 isAuthenticated 转发，导致该接口恒返回 400 空响应、商城货源流全站为空；
        ///   改为可空后不再参与必填校验，业务侧按 ?? false 兜底，端点仍以登录态覆盖。
        /// </summary>
        public bool? IsAuthenticated { get; set; }

        /// <summary>页码</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页条数</summary>
        public int PageSize { get; set; } = 20;
    }
}
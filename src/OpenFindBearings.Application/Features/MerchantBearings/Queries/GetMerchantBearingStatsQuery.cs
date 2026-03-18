using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Queries
{
    /// <summary>
    /// 获取商家-轴承关联统计信息查询
    /// </summary>
    public class GetMerchantBearingStatsQuery : IRequest<MerchantBearingStatsDto>
    {
        /// <summary>
        /// 商家ID（可选，统计指定商家）
        /// </summary>
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 轴承ID（可选，统计指定轴承）
        /// </summary>
        public Guid? BearingId { get; set; }
    }

    /// <summary>
    /// 商家-轴承关联统计DTO
    /// </summary>
    public class MerchantBearingStatsDto
    {
        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 在售数量
        /// </summary>
        public int OnSaleCount { get; set; }

        /// <summary>
        /// 待审核数量
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// 推荐数量
        /// </summary>
        public int FeaturedCount { get; set; }

        /// <summary>
        /// 商家数量（按商家统计时）
        /// </summary>
        public int MerchantCount { get; set; }

        /// <summary>
        /// 轴承数量（按轴承统计时）
        /// </summary>
        public int BearingCount { get; set; }
    }
}

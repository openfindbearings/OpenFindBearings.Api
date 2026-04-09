namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 轴承浏览历史DTO
    /// </summary>
    public class BearingHistoryDto
    {
        /// <summary>
        /// 历史记录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; set; }

        /// <summary>
        /// 轴承型号
        /// </summary>
        public string BearingCurrentCode { get; set; } = string.Empty;

        /// <summary>
        /// 轴承名称
        /// </summary>
        public string BearingName { get; set; } = string.Empty;

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string? BrandName { get; set; }

        /// <summary>
        /// 浏览时间
        /// </summary>
        public DateTime ViewedAt { get; set; }

        /// <summary>
        /// 浏览次数（累计）
        /// </summary>
        public int ViewCount { get; set; }
    }
}

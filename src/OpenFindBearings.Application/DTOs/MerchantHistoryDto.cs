namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商家浏览历史DTO
    /// </summary>
    public class MerchantHistoryDto
    {
        /// <summary>
        /// 历史记录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; set; }

        /// <summary>
        /// 商家名称
        /// </summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>
        /// 公司全称
        /// </summary>
        public string? CompanyName { get; set; }

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

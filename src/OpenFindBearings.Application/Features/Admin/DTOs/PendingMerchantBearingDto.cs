using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.DTOs;

namespace OpenFindBearings.Application.Features.Admin.DTOs
{
    /// <summary>
    /// 待审核商家产品DTO（管理员用）
    /// </summary>
    public class PendingMerchantBearingDto
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 商家信息
        /// </summary>
        public MerchantDto Merchant { get; set; } = null!;

        /// <summary>
        /// 轴承信息
        /// </summary>
        public BearingDto Bearing { get; set; } = null!;

        /// <summary>
        /// 价格描述
        /// </summary>
        public string? PriceDescription { get; set; }

        /// <summary>
        /// 库存描述
        /// </summary>
        public string? StockDescription { get; set; }

        /// <summary>
        /// 最小起订量描述
        /// </summary>
        public string? MinOrderDescription { get; set; }

        /// <summary>
        /// 商家备注
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 提交时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 提交人（商家员工）
        /// </summary>
        public string? SubmitterName { get; set; }
    }
}

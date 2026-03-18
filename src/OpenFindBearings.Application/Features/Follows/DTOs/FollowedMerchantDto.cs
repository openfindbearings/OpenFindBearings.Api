using OpenFindBearings.Application.Features.Merchants.DTOs;

namespace OpenFindBearings.Application.Features.Follows.DTOs
{
    /// <summary>
    /// 用户关注商家DTO
    /// 用于"我的关注"列表
    /// </summary>
    public class FollowedMerchantDto
    {
        /// <summary>
        /// 关注记录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 关注时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 关注的商家信息
        /// </summary>
        public MerchantDto Merchant { get; set; } = null!;
    }
}

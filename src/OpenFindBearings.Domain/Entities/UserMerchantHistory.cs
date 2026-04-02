namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户浏览商家历史实体
    /// 记录用户查看商家的历史记录，用于"浏览历史"功能
    /// 对应接口：POST /api/history/merchants/{merchantId}、GET /api/history/merchants
    /// </summary>
    public class UserMerchantHistory : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// 浏览商家的用户
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 商家ID
        /// 被浏览的商家
        /// </summary>
        public Guid MerchantId { get; private set; }

        /// <summary>
        /// 商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 查看时间
        /// 记录用户最后一次查看此商家的时间
        /// </summary>
        public DateTime ViewedAt { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private UserMerchantHistory() { }

        /// <summary>
        /// 创建浏览历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="merchantId">商家ID</param>
        /// <exception cref="ArgumentException">当参数为空时抛出</exception>
        public UserMerchantHistory(Guid userId, Guid merchantId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (merchantId == Guid.Empty)
                throw new ArgumentException("商家ID不能为空", nameof(merchantId));

            UserId = userId;
            MerchantId = merchantId;
            ViewedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新查看时间
        /// 当用户再次查看同一商家时，更新时间戳
        /// </summary>
        public void UpdateViewTime()
        {
            ViewedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户关注商家实体
    /// 记录用户关注的商家，用于"我的关注"功能
    /// 对应接口：POST /api/favorites/merchants/{merchantId}、GET /api/favorites/merchants
    /// </summary>
    public class UserMerchantFollow : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// 关联到关注此商家的用户
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 商家ID
        /// 被关注的商家
        /// </summary>
        public Guid MerchantId { get; private set; }

        /// <summary>
        /// 商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private UserMerchantFollow() { }

        /// <summary>
        /// 创建用户关注
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="merchantId">商家ID</param>
        /// <exception cref="ArgumentException">当参数为空时抛出</exception>
        public UserMerchantFollow(Guid userId, Guid merchantId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (merchantId == Guid.Empty)
                throw new ArgumentException("商家ID不能为空", nameof(merchantId));

            UserId = userId;
            MerchantId = merchantId;
        }
    }
}

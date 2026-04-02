namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户收藏轴承实体
    /// 记录用户收藏的轴承，用于"我的收藏"功能
    /// 对应接口：POST /api/favorites/bearings/{bearingId}、GET /api/favorites/bearings
    /// </summary>
    public class UserBearingFavorite : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// 关联到收藏此轴承的用户
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 轴承ID
        /// 被收藏的轴承
        /// </summary>
        public Guid BearingId { get; private set; }

        /// <summary>
        /// 轴承导航属性
        /// </summary>
        public Bearing? Bearing { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private UserBearingFavorite() { }

        /// <summary>
        /// 创建用户收藏
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="bearingId">轴承ID</param>
        /// <exception cref="ArgumentException">当参数为空时抛出</exception>
        public UserBearingFavorite(Guid userId, Guid bearingId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (bearingId == Guid.Empty)
                throw new ArgumentException("轴承ID不能为空", nameof(bearingId));

            UserId = userId;
            BearingId = bearingId;
        }
    }
}

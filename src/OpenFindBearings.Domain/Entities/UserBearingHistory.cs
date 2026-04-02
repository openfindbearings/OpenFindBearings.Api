namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户浏览轴承历史实体
    /// 记录用户查看轴承的历史记录，用于"浏览历史"功能
    /// 对应接口：POST /api/history/bearings/{bearingId}、GET /api/history/bearings
    /// </summary>
    public class UserBearingHistory : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// 浏览轴承的用户
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// 轴承ID
        /// 被浏览的轴承
        /// </summary>
        public Guid BearingId { get; private set; }

        /// <summary>
        /// 轴承导航属性
        /// </summary>
        public Bearing? Bearing { get; private set; }

        /// <summary>
        /// 查看时间
        /// 记录用户最后一次查看此轴承的时间
        /// </summary>
        public DateTime ViewedAt { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private UserBearingHistory() { }

        /// <summary>
        /// 创建浏览历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="bearingId">轴承ID</param>
        /// <exception cref="ArgumentException">当参数为空时抛出</exception>
        public UserBearingHistory(Guid userId, Guid bearingId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (bearingId == Guid.Empty)
                throw new ArgumentException("轴承ID不能为空", nameof(bearingId));

            UserId = userId;
            BearingId = bearingId;
            ViewedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新查看时间
        /// 当用户再次查看同一轴承时，更新时间戳
        /// </summary>
        public void UpdateViewTime()
        {
            ViewedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}

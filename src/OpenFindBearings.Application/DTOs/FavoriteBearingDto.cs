namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 用户收藏轴承DTO
    /// 用于"我的收藏"列表
    /// </summary>
    public class FavoriteBearingDto
    {
        /// <summary>
        /// 收藏记录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 收藏时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 收藏的轴承信息
        /// </summary>
        public BearingDto Bearing { get; set; } = null!;
    }
}

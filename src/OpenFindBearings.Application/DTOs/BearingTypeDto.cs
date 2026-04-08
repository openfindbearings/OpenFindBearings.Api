namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 轴承类型DTO
    /// </summary>
    public class BearingTypeDto
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 类型代码（如 DGBB）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 类型名称（如 深沟球轴承）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 类型描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 轴承数量统计
        /// </summary>
        public int BearingCount { get; set; }
    }
}

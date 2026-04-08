namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 系统配置DTO
    /// </summary>
    public class SystemConfigDto
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 配置键
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 配置分组
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        public string? UpdatedBy { get; set; }
    }
}

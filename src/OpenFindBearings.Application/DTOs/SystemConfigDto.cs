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
        /// 配置值类型（string / int / bool）
        /// 改动说明：原为实体上的元数据字段但从未下发到前端，管理端只能按纯文本编辑，
        ///           把布尔配置填成 "0" 会被静默回退为 true。下发后前端可据此渲染对应控件
        /// </summary>
        public string ValueType { get; set; } = "string";

        /// <summary>
        /// 是否为系统内置配置（不可删除）
        /// 改动说明：与 ValueType 同为实体元数据，此前未下发；下发后前端可对内置项隐藏删除入口
        /// </summary>
        public bool IsSystem { get; set; }

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

using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 系统配置实体
    /// </summary>
    public class SystemConfig : BaseEntity
    {
        /// <summary>
        /// 配置键
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        public string Value { get; private set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 配置分组
        /// </summary>
        public string Group { get; private set; } = string.Empty;

        /// <summary>
        /// 值类型（string/int/bool/json）
        /// </summary>
        public string ValueType { get; private set; } = "string";

        /// <summary>
        /// 是否系统级（不可删除）
        /// </summary>
        public bool IsSystem { get; private set; }

        /// <summary>
        /// 更新人ID
        /// </summary>
        public Guid? UpdatedBy { get; private set; }

        /// <summary>
        /// 更新人导航属性
        /// </summary>
        public User? Updater { get; private set; }

        private SystemConfig() { }

        public SystemConfig(
            string key,
            string value,
            string group,
            string? description = null,
            string valueType = "string",
            bool isSystem = false)
        {
            Key = key;
            Value = value;
            Group = group;
            Description = description;
            ValueType = valueType;
            IsSystem = isSystem;
        }

        public void UpdateValue(string value, Guid updatedBy)
        {
            Value = value;
            UpdatedBy = updatedBy;
            UpdateTimestamp();
        }
    }
}

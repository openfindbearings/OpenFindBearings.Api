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

        /// <summary>英文名称</summary>
        public string? EnglishName { get; set; }
        /// <summary>结构特点</summary>
        public string? StructureFeatures { get; set; }
        /// <summary>载荷特性</summary>
        public string? LoadCharacteristics { get; set; }
        /// <summary>典型型号前缀</summary>
        public string? TypicalPrefixes { get; set; }
        /// <summary>主要应用领域</summary>
        public string? MainApplications { get; set; }
        /// <summary>是否可分离结构</summary>
        public bool? IsSeparable { get; set; }
        /// <summary>结构示意图 URL</summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 轴承数量统计
        /// </summary>
        public int BearingCount { get; set; }
    }
}

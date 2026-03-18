namespace OpenFindBearings.Application.Features.Sync.DTOs
{
    /// <summary>
    /// 同步替代品关系DTO
    /// </summary>
    public class SyncInterchangeDto
    {
        /// <summary>
        /// 源轴承型号
        /// </summary>
        public string SourcePartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 源品牌代码
        /// </summary>
        public string SourceBrandCode { get; set; } = string.Empty;

        /// <summary>
        /// 目标轴承型号
        /// </summary>
        public string TargetPartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 目标品牌代码
        /// </summary>
        public string TargetBrandCode { get; set; } = string.Empty;

        /// <summary>
        /// 替代类型
        /// </summary>
        public string InterchangeType { get; set; } = "exact";

        /// <summary>
        /// 可信度
        /// </summary>
        public int Confidence { get; set; } = 80;

        /// <summary>
        /// 数据来源
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 是否双向替代
        /// </summary>
        public bool IsBidirectional { get; set; } = true;
    }
}

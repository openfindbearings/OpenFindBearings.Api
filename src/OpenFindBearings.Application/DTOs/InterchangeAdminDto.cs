namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 替代关系管理行 DTO（v2.16.0）：平台管理列表 / 审核队列 / 商家提交进度共用。
    /// 含源/目标轴承精简信息、三级类型、审核状态与驳回理由。
    /// </summary>
    public class InterchangeAdminDto
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; set; }

        /// <summary>源轴承（被替代方）精简信息</summary>
        public InterchangeBearingRefDto SourceBearing { get; set; } = new();

        /// <summary>目标轴承（替代方）精简信息</summary>
        public InterchangeBearingRefDto TargetBearing { get; set; } = new();

        /// <summary>替代类型（exact/conditional/functional）</summary>
        public string? InterchangeType { get; set; }

        /// <summary>替代类型中文标签</summary>
        public string TypeLabel { get; set; } = string.Empty;

        /// <summary>可信度 0-100</summary>
        public int Confidence { get; set; }

        /// <summary>来源说明（如"SKF官方互换表"，对应实体 Source 字段）</summary>
        public string? Origin { get; set; }

        /// <summary>备注</summary>
        public string? Remarks { get; set; }

        /// <summary>是否双向替代</summary>
        public bool IsBidirectional { get; set; }

        /// <summary>审核状态（0=已生效 1=待审核 2=已驳回）</summary>
        public int ReviewStatus { get; set; }

        /// <summary>审核状态中文标签</summary>
        public string ReviewStatusLabel { get; set; } = string.Empty;

        /// <summary>驳回理由（仅已驳回有值）</summary>
        public string? ReviewComment { get; set; }

        /// <summary>提出候选的商户ID（平台自建为 null）</summary>
        public Guid? ProposedByMerchantId { get; set; }

        /// <summary>提出候选的商户名称（平台自建为 null）</summary>
        public string? ProposedByMerchantName { get; set; }

        /// <summary>数据来源类型（Manual/Crawler/UserSubmitted 等）</summary>
        public string? DataSourceType { get; set; }

        /// <summary>创建时间（UTC，展示层转本地）</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>更新时间（UTC）</summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 替代关系中的轴承精简信息
    /// </summary>
    public class InterchangeBearingRefDto
    {
        /// <summary>轴承ID</summary>
        public Guid Id { get; set; }

        /// <summary>型号</summary>
        public string PartNumber { get; set; } = string.Empty;

        /// <summary>品牌名</summary>
        public string? BrandName { get; set; }

        /// <summary>轴承类型名</summary>
        public string? BearingType { get; set; }

        /// <summary>内径</summary>
        public decimal InnerDiameter { get; set; }

        /// <summary>外径</summary>
        public decimal OuterDiameter { get; set; }

        /// <summary>宽度</summary>
        public decimal Width { get; set; }
    }
}
namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 轴承详情-替代品条目（v2.16.0 替代品治理）。
    /// 表示"某个替代品可替换当前查询轴承"的一条建议，含三级替代类型与责任提示所需字段。
    /// </summary>
    public class BearingInterchangeDto
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; set; }

        /// <summary>替代品（对方）轴承ID，供前端导航</summary>
        public Guid BearingId { get; set; }

        /// <summary>替代品型号</summary>
        public string PartNumber { get; set; } = string.Empty;

        /// <summary>替代品品牌名</summary>
        public string? BrandName { get; set; }

        /// <summary>替代品轴承类型</summary>
        public string? BearingType { get; set; }

        /// <summary>替代品内径（便于判断 Pin-to-Pin 兼容）</summary>
        public decimal InnerDiameter { get; set; }

        /// <summary>替代品外径</summary>
        public decimal OuterDiameter { get; set; }

        /// <summary>替代品宽度</summary>
        public decimal Width { get; set; }

        /// <summary>替代类型（exact/conditional/functional）</summary>
        public string? InterchangeType { get; set; }

        /// <summary>替代类型中文标签（标准等效替代/条件替代（需评估）/仅供参考）</summary>
        public string TypeLabel { get; set; } = string.Empty;

        /// <summary>可信度 0-100</summary>
        public int Confidence { get; set; }

        /// <summary>备注（如"需更换密封圈"）</summary>
        public string? Remarks { get; set; }

        /// <summary>是否双向替代</summary>
        public bool IsBidirectional { get; set; }

        /// <summary>是否可进自动推荐（"仅供参考"档为 false）</summary>
        public bool IsAutoRecommendable { get; set; }
    }
}
namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 轴承列表项DTO
    /// </summary>
    public class BearingDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 现行代号
        /// </summary>
        public string PartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 曾用代号
        /// </summary>
        public string? OldNumber { get; set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 轴承类型名称
        /// </summary>
        public string BearingType { get; set; } = string.Empty;

        // 尺寸参数
        public decimal InnerDiameter { get; set; }
        public decimal OuterDiameter { get; set; }
        public decimal Width { get; set; }
        public decimal? Weight { get; set; }

        // 品牌信息
        public Guid BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? BrandCountry { get; set; }

        // 产地和类别信息
        public string? OriginCountry { get; set; }
        public string Category { get; set; } = string.Empty;

        // 类型信息
        public Guid BearingTypeId { get; set; }
        public string BearingTypeName { get; set; } = string.Empty;

        // 是否标准
        public bool IsStandard { get; set; }

        // 统计
        public int ViewCount { get; set; }
        public int FavoriteCount { get; set; }

        // 图片
        public string? Image3DUrl { get; set; }
        public string? Image2DUrl { get; set; }

        /// <summary>
        /// 参数来源（v1.6.0 数据可信联动）：Internet/OcrExtract/SeedData=仅供参考，其余=可信。
        /// 供前端对参数区标注"参数来源：互联网 · 仅供参考"
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// 创建时间（UTC）。
        /// 改动说明（v1.50.0 下钻筛选）：Admin 轴承列表展示"创建时间"列并支持按下钻起点核对。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 该型号"认证商家有货"家数（v2.22.3 搜索结果分流）。
        /// 口径与寻货匹配器一致（公域可见商品 + 已认证 + 生效 + 未软删，按商家去重）。
        /// 大于 0 时搜索结果提供「寻货」入口定向邀请；为 0 时仅按互联网数据公布，不提供寻货入口。
        /// </summary>
        public int VerifiedMerchantCount { get; set; }
    }
}

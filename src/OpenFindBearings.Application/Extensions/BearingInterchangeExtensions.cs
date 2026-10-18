using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    /// <summary>
    /// 替代关系映射扩展：实体到公域条目 DTO / 管理行 DTO 的转换，
    /// 以及三级替代类型的中文标签口径（唯一来源，避免各处重复）。
    /// 改动说明（v2.17.0 替代品字典化）：删除审核状态标签口径（状态机已取消），同步去掉 Enums 引用
    /// </summary>
    public static class BearingInterchangeExtensions
    {
        /// <summary>
        /// 三级替代类型中文标签（唯一口径）：
        /// exact=标准等效替代；conditional=条件替代（需评估）；functional=仅供参考
        /// </summary>
        public static string ToTypeLabel(string? type) => type switch
        {
            "exact" => "标准等效替代",
            "conditional" => "条件替代（需评估）",
            "functional" => "仅供参考",
            _ => type ?? "未知"
        };

        /// <summary>
        /// 转换为公域替代品条目（viewedBearingId 为当前查看的轴承）：
        /// 替代品取"对方"轴承——若查看方为源则取目标，否则（双向反向）取源
        /// </summary>
        public static BearingInterchangeDto ToInterchangeDto(this BearingInterchange bi, Guid viewedBearingId)
        {
            var substitute = bi.SourceBearingId == viewedBearingId ? bi.TargetBearing : bi.SourceBearing;

            return new BearingInterchangeDto
            {
                Id = bi.Id,
                BearingId = substitute?.Id ?? Guid.Empty,
                PartNumber = substitute?.PartNumber ?? string.Empty,
                BrandName = substitute?.Brand?.Name,
                BearingType = substitute?.BearingType,
                InnerDiameter = substitute?.Dimensions?.InnerDiameter ?? 0,
                OuterDiameter = substitute?.Dimensions?.OuterDiameter ?? 0,
                Width = substitute?.Dimensions?.Width ?? 0,
                InterchangeType = bi.InterchangeType,
                TypeLabel = ToTypeLabel(bi.InterchangeType),
                Confidence = bi.Confidence,
                Remarks = bi.Remarks,
                IsBidirectional = bi.IsBidirectional,
                IsAutoRecommendable = bi.IsAutoRecommendable
            };
        }

        /// <summary>转换为平台管理行 DTO</summary>
        public static InterchangeAdminDto ToAdminDto(this BearingInterchange bi)
        {
            return new InterchangeAdminDto
            {
                Id = bi.Id,
                SourceBearing = ToRef(bi.SourceBearing),
                TargetBearing = ToRef(bi.TargetBearing),
                InterchangeType = bi.InterchangeType,
                TypeLabel = ToTypeLabel(bi.InterchangeType),
                Confidence = bi.Confidence,
                Origin = bi.Source,
                Remarks = bi.Remarks,
                IsBidirectional = bi.IsBidirectional,
                DataSourceType = bi.DataSource?.SourceType?.ToString(),
                CreatedAt = bi.CreatedAt,
                UpdatedAt = bi.UpdatedAt ?? bi.CreatedAt
            };
        }

        /// <summary>轴承精简引用</summary>
        private static InterchangeBearingRefDto ToRef(Bearing? b) => b == null
            ? new InterchangeBearingRefDto()
            : new InterchangeBearingRefDto
            {
                Id = b.Id,
                PartNumber = b.PartNumber,
                BrandName = b.Brand?.Name,
                BearingType = b.BearingType,
                InnerDiameter = b.Dimensions?.InnerDiameter ?? 0,
                OuterDiameter = b.Dimensions?.OuterDiameter ?? 0,
                Width = b.Dimensions?.Width ?? 0
            };
    }
}
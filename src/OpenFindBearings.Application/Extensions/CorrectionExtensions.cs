using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class CorrectionExtensions
    {
        private static readonly Dictionary<string, string> FieldDisplayNames = new()
        {
            { "CurrentCode", "型号" },
            { "Name", "名称" },
            { "Phone", "电话" },
            { "Address", "地址" },
            { "InnerDiameter", "内径" },
            { "OuterDiameter", "外径" },
            { "Width", "宽度" },
            { "Price", "价格" },
            { "Brand", "品牌" },
            { "BearingType", "轴承类型" }
        };

        public static CorrectionDto ToDto(this CorrectionRequest correction)
        {
            var targetDisplay = correction.TargetType switch
            {
                "Bearing" => correction.Bearing?.CurrentCode ?? correction.TargetId.ToString(),
                "Merchant" => correction.Merchant?.Name ?? correction.TargetId.ToString(),
                _ => correction.TargetId.ToString()
            };

            return new CorrectionDto
            {
                Id = correction.Id,
                TargetType = correction.TargetType,
                TargetId = correction.TargetId,
                TargetDisplay = targetDisplay,
                FieldName = correction.FieldName,
                FieldDisplayName = FieldDisplayNames.GetValueOrDefault(correction.FieldName, correction.FieldName),
                OriginalValue = correction.OriginalValue,
                SuggestedValue = correction.SuggestedValue,
                Reason = correction.Reason,
                SubmittedBy = correction.SubmittedBy,
                SubmitterName = correction.Submitter?.Nickname ?? "未知用户",
                SubmittedAt = correction.SubmittedAt,
                Status = correction.Status.ToString(),
                ReviewedBy = correction.ReviewedBy,
                ReviewerName = correction.Reviewer?.Nickname,
                ReviewedAt = correction.ReviewedAt,
                ReviewComment = correction.ReviewComment
            };
        }
    }
}
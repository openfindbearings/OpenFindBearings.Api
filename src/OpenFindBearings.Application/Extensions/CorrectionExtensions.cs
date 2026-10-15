using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class CorrectionExtensions
    {
        // 改动说明（v2.13.2 标签收敛）：删除本地 PascalCase FieldDisplayNames（与库中 camelCase 字段名
        //   永不匹配、恒回退原值），统一走 Domain.CorrectionFieldLabels
        public static CorrectionDto ToDto(this CorrectionRequest correction)
        {
            var targetDisplay = correction.TargetType switch
            {
                "Bearing" => correction.Bearing?.PartNumber ?? correction.TargetId.ToString(),
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
                FieldDisplayName = CorrectionFieldLabels.Label(correction.TargetType, correction.FieldName),
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
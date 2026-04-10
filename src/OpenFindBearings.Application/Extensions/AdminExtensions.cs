using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class AdminExtensions
    {
        public static AuditLogDto ToDto(this AuditLog log)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                EntityName = string.Empty,
                OperatorId = log.OperatorId,
                OperatorName = log.Operator?.Nickname ?? "未知",
                OperatedAt = log.OperatedAt,
                Details = log.AfterData,
                Remarks = log.Remarks
            };
        }

        public static PendingLicenseDto ToDto(this LicenseVerification license)
        {
            return new PendingLicenseDto
            {
                Id = license.Id,
                MerchantId = license.MerchantId,
                MerchantName = license.Merchant?.Name ?? string.Empty,
                LicenseUrl = license.LicenseUrl ?? string.Empty,
                Status = license.Status.ToString(),
                SubmittedBy = license.SubmittedBy,
                SubmitterName = license.Submitter?.Nickname ?? "未知",
                SubmittedAt = license.SubmittedAt
            };
        }
    }
}
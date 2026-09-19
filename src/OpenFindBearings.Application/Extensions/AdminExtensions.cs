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

        /// <summary>
        /// 证照材料实体转队列 DTO（v2.7.0 泛化：带材料类型与中文名）
        /// </summary>
        public static PendingDocumentDto ToDto(this MerchantDocument document)
        {
            return new PendingDocumentDto
            {
                Id = document.Id,
                MerchantId = document.MerchantId,
                MerchantName = document.Merchant?.Name ?? string.Empty,
                Type = (int)document.Type,
                TypeName = Application.DTOs.DocumentRequirements.DisplayName(document.Type),
                FileUrl = document.FileUrl ?? string.Empty,
                Status = document.Status.ToString(),
                SubmittedBy = document.SubmittedBy,
                SubmitterName = document.Submitter?.Nickname ?? "未知",
                SubmittedAt = document.SubmittedAt
            };
        }
    }
}
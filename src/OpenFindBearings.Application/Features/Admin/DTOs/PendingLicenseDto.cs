namespace OpenFindBearings.Application.Features.Admin.DTOs
{
    public class PendingLicenseDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public string LicenseUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid SubmittedBy { get; set; }
        public string SubmitterName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }
}

namespace OpenFindBearings.Application.Features.Sync.DTOs
{
    /// <summary>
    /// 同步商家DTO
    /// </summary>
    public class SyncMerchantDto
    {
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? UnifiedSocialCreditCode { get; set; }
        public int Type { get; set; }
        public string? Description { get; set; }
        public string? BusinessScope { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsVerified { get; set; }
    }
}

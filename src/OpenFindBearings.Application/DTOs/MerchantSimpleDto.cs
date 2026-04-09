namespace OpenFindBearings.Application.DTOs
{
    public class MerchantLightDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}

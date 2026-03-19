namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    public class MerchantSimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}

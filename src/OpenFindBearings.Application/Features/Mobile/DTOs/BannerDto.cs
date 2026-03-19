namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    public class BannerDto
    {
        public string Id { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public string? Title { get; set; }
    }
}

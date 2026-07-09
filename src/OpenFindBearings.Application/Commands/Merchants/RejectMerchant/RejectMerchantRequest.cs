using System.Text.Json.Serialization;

namespace OpenFindBearings.Application.Commands.Merchants.RejectMerchant
{
    public class RejectMerchantRequest
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}

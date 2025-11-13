using System.Text.Json.Serialization;

namespace PaymentGateway.Application.Models.Requests;

public sealed class AcquiringRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; } = string.Empty;

    [JsonIgnore] // used only to compute ExpiryDate
    public int ExpiryMonth { get; init; }

    [JsonIgnore]
    public int ExpiryYear { get; init; }

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate => $"{ExpiryMonth:D2}/{ExpiryYear:D4}";

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string Cvv { get; init; } = string.Empty;
}

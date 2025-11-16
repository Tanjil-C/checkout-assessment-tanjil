using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PaymentGateway.Application;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Infrastructure;

public class AcquiringBankHttpClient : IAcquiringBankHttpClient
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = $"{PaymentConstants.Endpoint}";
    public AcquiringBankHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<AcquiringStatus> AuthorizeAsync(AcquiringRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(Endpoint, request);

        if (!response.IsSuccessStatusCode)
            return AcquiringStatus.Declined;

        var body = await response.Content.ReadFromJsonAsync<BankResponse>();

        if (body?.Authorized == true) return AcquiringStatus.Authorized;

        return AcquiringStatus.Declined;

    }
}

public class BankResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }
    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }
}

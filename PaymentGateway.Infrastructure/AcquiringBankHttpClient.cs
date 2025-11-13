using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Infrastructure;

public class AcquiringBankHttpClient : IAcquiringBankHttpClient
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "payments";
    public AcquiringBankHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<AcquiringResult> AuthorizeAsync(AcquiringRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(Endpoint, request);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return new AcquiringResult(AcquiringStatus.BadRequest, null);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            return new AcquiringResult(AcquiringStatus.Unavailable, null);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<BankResponse>();

        return new AcquiringResult(
            body?.Authorized == true ? AcquiringStatus.Authorized : AcquiringStatus.Declined,
            body?.AuthorizationCode
        );

    }
}

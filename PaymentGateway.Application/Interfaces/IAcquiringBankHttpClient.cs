using PaymentGateway.Application.Models.Requests;
using PaymentGateway.Application.Models.Responses;

namespace PaymentGateway.Application.Interfaces;

public interface IAcquiringBankHttpClient
{
    Task<AcquiringResult> AuthorizeAsync(AcquiringRequest request);
}

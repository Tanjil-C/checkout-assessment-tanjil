using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Interfaces;

public interface IAcquiringBankHttpClient
{
    Task<AcquiringStatus> AuthorizeAsync(AcquiringRequest request);
}

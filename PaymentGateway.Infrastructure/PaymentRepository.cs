using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Infrastructure;

public class PaymentRepository : IPaymentRepository
{
    private static readonly List<Payment> _payments = new()
    {
        new Payment
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Status = AcquiringStatus.Authorized,
            CardLast4 = "4242",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "GBP",
            Amount = 1050
        },
        new Payment
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Status = AcquiringStatus.Declined,
            CardLast4 = "1234",
            ExpiryMonth = 6,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 2500
        },
        new Payment
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Status = AcquiringStatus.Authorized,
            CardLast4 = "9876",
            ExpiryMonth = 3,
            ExpiryYear = 2027,
            Currency = "EUR",
            Amount = 199
        }
    };

    public Task<Guid> SaveAsync(CreatePaymentCommand command, AcquiringStatus status)
    {
        var payment = new Payment()
        {
            Id = Guid.NewGuid(),
            Status = status,
            CardLast4 = command.CardNumber.Substring(command.CardNumber.Length - 4),
            ExpiryMonth = command.ExpiryMonth,
            ExpiryYear = command.ExpiryYear,
            Currency = command.Currency,
            Amount = command.Amount,
        };

        _payments.Add(payment);

        return Task.FromResult(payment.Id);
    }
}

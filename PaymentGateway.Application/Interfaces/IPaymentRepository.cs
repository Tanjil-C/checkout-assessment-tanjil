using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Guid> SaveAsync(CreatePaymentCommand command, AcquiringStatus status);
}

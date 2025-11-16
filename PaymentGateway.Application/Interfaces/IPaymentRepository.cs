using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Guid> SaveAsync(CreatePaymentCommand command, AcquiringStatus status);
    Task<Payment> GetPayment(Guid id);
}

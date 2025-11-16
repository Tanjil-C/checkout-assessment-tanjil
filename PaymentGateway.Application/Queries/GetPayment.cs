using FluentValidation;
using MediatR;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Queries;

public record GetPaymentCommand(Guid Id) : IRequest<PaymentResponseDto<Payment>> { }

public class GetPaymentValidator : AbstractValidator<GetPaymentCommand>
{
    public GetPaymentValidator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
    }
}

public class GetPaymentHandler : IRequestHandler<GetPaymentCommand, PaymentResponseDto<Payment>>
{
    private readonly IPaymentRepository _paymentRepository;
    public GetPaymentHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository; 
    }
    public async Task<PaymentResponseDto<Payment>> Handle(GetPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetPayment(request.Id);

        if (payment.Id == Guid.Empty) return new PaymentResponseDto<Payment>();

        return new PaymentResponseDto<Payment>()
        {
            Value = payment,
            AcquiringStatus = Domain.Enums.AcquiringStatus.Authorized,
        };

    }
}
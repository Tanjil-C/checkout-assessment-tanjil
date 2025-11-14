
using FluentValidation;
using MediatR;
using PaymentGateway.Application.Commands.CreatePayment;

namespace PaymentGateway.Api.Middleware;

public class CreatePaymentValidationBehaviour : IPipelineBehavior<CreatePaymentCommand, PaymentResponseDto>
{
    private readonly IValidator<CreatePaymentCommand> _validator;
    public CreatePaymentValidationBehaviour(IValidator<CreatePaymentCommand> validator)
    {
        _validator = validator;
    }
    public async Task<PaymentResponseDto> Handle(CreatePaymentCommand request, RequestHandlerDelegate<PaymentResponseDto> next, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            return new PaymentResponseDto()
            {
                Id = Guid.Empty,
                AcquiringStatus = Domain.Enums.AcquiringStatus.Rejected
            };
        }
        return await next();
    }
}

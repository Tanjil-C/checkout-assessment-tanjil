using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using PaymentGateway.Application.Commands.Rules;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Enums;
namespace PaymentGateway.Application.Commands.CreatePayment;

public record PaymentCommandDto
{
    public string CardNumber { get; init; } = default!;
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public string Currency { get; init; } = default!;
    public int Amount { get; init; }
    public string Cvv { get; init; } = default!;

    [JsonIgnore]
    public string NormalizedCardNumber => string.IsNullOrWhiteSpace(CardNumber)
        ? string.Empty : CardNumber.Replace(" ", "").Replace("-", "");
}

public record PaymentResponseDto
{
    public Guid Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AcquiringStatus AcquiringStatus { get; set; }
}

public record CreatePaymentCommand : PaymentCommandDto, IRequest<PaymentResponseDto>
{
}

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{

    public CreatePaymentValidator(ICurrencyService currencyService)
    {
        RuleFor(x => x.CardNumber)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Card Number cannot be null.")
            .NotEmpty().WithMessage("Card Number cannot be empty.")
            .Matches(@"^\d{14,19}$").WithMessage("Card Number must be numeric and between 14 and 19 digits.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage("Expiry Month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Expiry Year must be the current year or later.");

        RuleFor(x => x)
            .Must(ExpiryRules.NotBeExpired)
            .WithMessage("Card expiry must be in the future.");

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Currency is required.")
            .NotEmpty().WithMessage("Currency is required.")
            .Matches(@"^[A-Za-z]{3}$").WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0 (minor currency units).");

        RuleFor(x => x.Cvv)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("CVV is required.")
            .NotEmpty().WithMessage("CVV is required.")
            .Matches(@"^\d{3,4}$").WithMessage("CVV must be numeric and 3 to 4 digits long.");
    }

    public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentResponseDto>
    {
        private readonly IsSupported _isSupported;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAcquiringBankHttpClient _acquiringBankHttpClient;

        public CreatePaymentHandler(IsSupported isSupported, IPaymentRepository paymentRepository, IAcquiringBankHttpClient acquiringBankHttpClient)
        {
            _isSupported = isSupported;
            _paymentRepository = paymentRepository;
            _acquiringBankHttpClient = acquiringBankHttpClient;
        }


        public async Task<PaymentResponseDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            var supported = await _isSupported.EvaluateAsync(request.Currency);
            if (!supported) throw new InvalidOperationException($"Unsupported currency: {request.Currency}");
            // Note: idempotency should be applied in production to prevent duplicate charges.
            // Omitted here for simplicity in this assessment.

            var acquiringRequest = new AcquiringRequest
            {
                CardNumber = request.NormalizedCardNumber,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };


            // In a production system, we would wrap the bank response in a richer object containing both the status and HTTP response code for logging, diagnostics, and traceability.  
            // For this assessment, that detail is omitted to keep the implementation focused and concise.
            var status = await _acquiringBankHttpClient.AuthorizeAsync(acquiringRequest);

            if (status != AcquiringStatus.Authorized)
            {
                return new PaymentResponseDto
                {
                    Id = Guid.Empty,
                    AcquiringStatus = AcquiringStatus.Declined
                };
            }
            var id = await _paymentRepository.SaveAsync(request, status);
            return new PaymentResponseDto() { Id = id, AcquiringStatus = status };
        }
    }
}
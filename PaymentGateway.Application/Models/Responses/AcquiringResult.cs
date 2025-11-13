using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Models.Responses;

public sealed record AcquiringResult(AcquiringStatus Status, string? AuthorizationCode);


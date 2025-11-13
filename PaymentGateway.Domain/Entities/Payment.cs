using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } 

    public AcquiringStatus Status { get; set; } 

    public string CardLast4 { get; set; } = string.Empty; 

    public int ExpiryMonth { get; set; }

    public int ExpiryYear { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int Amount { get; set; }
}

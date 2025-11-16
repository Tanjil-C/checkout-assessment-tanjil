using PaymentGateway.Application.Commands.CreatePayment;

namespace PaymentGateway.Application.Commands.Rules;

public static class ExpiryRules
{
    public static bool NotBeExpired(PaymentCommandDto dto)
    {
        if (dto.ExpiryMonth < 1 || dto.ExpiryMonth > 12) return true; 
        var now = DateTime.UtcNow;
        if (dto.ExpiryYear < now.Year) return false;
        if (dto.ExpiryYear > now.Year) return true;
        return dto.ExpiryMonth >= now.Month; 
    }
}
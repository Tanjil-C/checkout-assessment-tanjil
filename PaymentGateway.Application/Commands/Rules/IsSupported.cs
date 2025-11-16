using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Application.Commands.Rules;

public class IsSupported
{
    private readonly ICurrencyService _currencyService;

    public IsSupported(ICurrencyService currencyService)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
    }

    public Task<bool> EvaluateAsync(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return Task.FromResult(false);
        var normalised = currency.Trim().ToUpperInvariant();
        return _currencyService.IsSupported(normalised);
    }
}
using FluentValidation;

using MediatR;

using PaymentGateway.Api.Middleware;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Commands.Rules;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Infrastructure;

namespace PaymentGateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePaymentCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreatePaymentValidator).Assembly);
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAcquiringBankHttpClient, AcquiringBankHttpClient>();
        services.AddSingleton<ICurrencyService, CurrencyService>();
        services.AddScoped<IsSupported>();
        services.AddTransient<IPipelineBehavior<CreatePaymentCommand, PaymentResponseDto>, CreatePaymentValidationBehaviour>();
        services.AddHttpClient<IAcquiringBankHttpClient, AcquiringBankHttpClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:8080/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
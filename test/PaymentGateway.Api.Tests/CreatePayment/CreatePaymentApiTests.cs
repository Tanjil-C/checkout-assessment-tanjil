using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.CreatePayment;

public class CreatePaymentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreatePaymentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayment_Should_Return_Ok_When_Request_Is_Valid_And_Authorized()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IAcquiringBankHttpClient registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(IAcquiringBankHttpClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockAcquiringBank = new Mock<IAcquiringBankHttpClient>();
                mockAcquiringBank.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
                    .ReturnsAsync(AcquiringStatus.Authorized);
                
                services.AddSingleton(mockAcquiringBank.Object);
            });
        }).CreateClient();

        var request = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
        result.Should().NotBeNull();
        result!.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePayment_Should_Return_Ok_With_Declined_Status_When_Bank_Declines()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IAcquiringBankHttpClient registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(IAcquiringBankHttpClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockAcquiringBank = new Mock<IAcquiringBankHttpClient>();
                mockAcquiringBank.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
                    .ReturnsAsync(AcquiringStatus.Declined);
                
                services.AddSingleton(mockAcquiringBank.Object);
            });
        }).CreateClient();

        var request = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
        result.Should().NotBeNull();
        result!.AcquiringStatus.Should().Be(AcquiringStatus.Declined);
        result.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task CreatePayment_Should_Return_Ok_With_Rejected_Status_When_Validation_Fails()
    {
        // Arrange
        var request = new CreatePaymentCommand
        {
            CardNumber = "123", // Invalid card number
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
        result.Should().NotBeNull();
        result!.AcquiringStatus.Should().Be(AcquiringStatus.Rejected);
        result.Value.Should().Be(Guid.Empty);
    }

    [Theory]
    [InlineData("", 12, 2025, "USD", 1000, "123")] // Empty card number
    [InlineData("1234567890123456", 0, 2025, "USD", 1000, "123")] // Invalid month
    [InlineData("1234567890123456", 12, 2020, "USD", 1000, "123")] // Past year
    [InlineData("1234567890123456", 12, 2025, "", 1000, "123")] // Empty currency
    [InlineData("1234567890123456", 12, 2025, "USD", 0, "123")] // Zero amount
    [InlineData("1234567890123456", 12, 2025, "USD", 1000, "")] // Empty CVV
    public async Task CreatePayment_Should_Return_Rejected_When_Required_Fields_Are_Invalid(
        string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv)
    {
        // Arrange
        var request = new CreatePaymentCommand
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            Currency = currency,
            Amount = amount,
            Cvv = cvv
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
        result.Should().NotBeNull();
        result!.AcquiringStatus.Should().Be(AcquiringStatus.Rejected);
        result.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task CreatePayment_Should_Handle_Unsupported_Currency()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing ICurrencyService registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(ICurrencyService)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockCurrencyService = new Mock<ICurrencyService>();
                mockCurrencyService.Setup(x => x.IsSupported(It.IsAny<string>()))
                    .ReturnsAsync(false);
                
                services.AddSingleton(mockCurrencyService.Object);
            });
        }).CreateClient();

        var request = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "XYZ", // Unsupported currency
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreatePayment_Should_Accept_Valid_Card_Numbers_Of_Different_Lengths()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IAcquiringBankHttpClient registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(IAcquiringBankHttpClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockAcquiringBank = new Mock<IAcquiringBankHttpClient>();
                mockAcquiringBank.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
                    .ReturnsAsync(AcquiringStatus.Authorized);
                
                services.AddSingleton(mockAcquiringBank.Object);
            });
        }).CreateClient();

        var cardNumbers = new[]
        {
            "12345678901234", // 14 digits
            "123456789012345", // 15 digits
            "1234567890123456", // 16 digits
            "12345678901234567", // 17 digits
            "123456789012345678", // 18 digits
            "1234567890123456789" // 19 digits
        };

        foreach (var cardNumber in cardNumbers)
        {
            var request = new CreatePaymentCommand
            {
                CardNumber = cardNumber,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
            result.Should().NotBeNull();
            result!.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        }
    }

    [Fact]
    public async Task CreatePayment_Should_Accept_Valid_CVV_Lengths()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IAcquiringBankHttpClient registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(IAcquiringBankHttpClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockAcquiringBank = new Mock<IAcquiringBankHttpClient>();
                mockAcquiringBank.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
                    .ReturnsAsync(AcquiringStatus.Authorized);
                
                services.AddSingleton(mockAcquiringBank.Object);
            });
        }).CreateClient();

        var cvvValues = new[] { "123", "1234" };

        foreach (var cvv in cvvValues)
        {
            var request = new CreatePaymentCommand
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 1000,
                Cvv = cvv
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
            result.Should().NotBeNull();
            result!.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        }
    }

    [Fact]
    public async Task CreatePayment_Should_Accept_Different_Supported_Currencies()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IAcquiringBankHttpClient registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(IAcquiringBankHttpClient)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var mockAcquiringBank = new Mock<IAcquiringBankHttpClient>();
                mockAcquiringBank.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
                    .ReturnsAsync(AcquiringStatus.Authorized);
                
                services.AddSingleton(mockAcquiringBank.Object);
            });
        }).CreateClient();

        var currencies = new[] { "USD", "GBP", "EUR" };

        foreach (var currency in currencies)
        {
            var request = new CreatePaymentCommand
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = currency,
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Guid>>();
            result.Should().NotBeNull();
            result!.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        }
    }
}
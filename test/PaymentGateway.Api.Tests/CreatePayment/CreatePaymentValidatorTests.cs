using FluentValidation.TestHelper;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Interfaces;
using Moq;
using FluentAssertions;

namespace PaymentGateway.Api.Tests.CreatePayment;

public class CreatePaymentValidatorTests
{
    private readonly CreatePaymentValidator _validator;
    private readonly Mock<ICurrencyService> _mockCurrencyService;

    public CreatePaymentValidatorTests()
    {
        _mockCurrencyService = new Mock<ICurrencyService>();
        _validator = new CreatePaymentValidator(_mockCurrencyService.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Have_Error_When_CardNumber_Is_Null_Or_Empty(string cardNumber)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData("123456789012")] // Too short (12 digits)
    [InlineData("1234567890123")] // Too short (13 digits)
    [InlineData("12345678901234567890")] // Too long (20 digits)
    [InlineData("12345678901234567890123")] // Too long (23 digits)
    public void Should_Have_Error_When_CardNumber_Length_Is_Invalid(string cardNumber)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData("12345678901234a5")] // Contains letter
    [InlineData("1234-5678-9012-3456")] // Contains dashes
    [InlineData("1234 5678 9012 3456")] // Contains spaces
    [InlineData("1234.5678.9012.3456")] // Contains dots
    public void Should_Have_Error_When_CardNumber_Contains_Non_Numeric_Characters(string cardNumber)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData("12345678901234")] // 14 digits
    [InlineData("123456789012345")] // 15 digits
    [InlineData("1234567890123456")] // 16 digits
    [InlineData("12345678901234567")] // 17 digits
    [InlineData("123456789012345678")] // 18 digits
    [InlineData("1234567890123456789")] // 19 digits
    public void Should_Not_Have_Error_When_CardNumber_Is_Valid(string cardNumber)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(25)]
    public void Should_Have_Error_When_ExpiryMonth_Is_Invalid(int expiryMonth)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = expiryMonth,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Should_Not_Have_Error_When_ExpiryMonth_Is_Valid(int expiryMonth)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = expiryMonth,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Fact]
    public void Should_Have_Error_When_ExpiryYear_Is_In_Past()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year - 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryYear);
    }

    [Fact]
    public void Should_Have_Error_When_Card_Is_Expired_This_Month()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = now.Month - (now.Month == 1 ? -11 : 1), // Previous month
            ExpiryYear = now.Month == 1 ? now.Year - 1 : now.Year,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Card expiry must be in the future.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Have_Error_When_Currency_Is_Null_Or_Empty(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = currency,
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("US")] // Too short
    [InlineData("USDX")] // Too long
    [InlineData("12A")] // Contains numbers
    [InlineData("U$D")] // Contains special characters
    public void Should_Have_Error_When_Currency_Format_Is_Invalid(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = currency,
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    [InlineData("usd")] // Should accept lowercase
    [InlineData("gbp")]
    public void Should_Not_Have_Error_When_Currency_Is_Valid(string currency)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = currency,
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_Amount_Is_Zero_Or_Negative(int amount)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = amount,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(10000)]
    public void Should_Not_Have_Error_When_Amount_Is_Positive(int amount)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = amount,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Have_Error_When_Cvv_Is_Null_Or_Empty(string cvv)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = cvv
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Cvv);
    }

    [Theory]
    [InlineData("12")] // Too short
    [InlineData("12345")] // Too long
    [InlineData("12a")] // Contains letter
    [InlineData("12$")] // Contains special character
    public void Should_Have_Error_When_Cvv_Format_Is_Invalid(string cvv)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = cvv
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Cvv);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Should_Not_Have_Error_When_Cvv_Is_Valid(string cvv)
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = cvv
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Cvv);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
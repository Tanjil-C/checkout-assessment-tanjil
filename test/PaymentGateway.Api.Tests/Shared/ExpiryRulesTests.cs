using FluentAssertions;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Commands.Rules;

namespace PaymentGateway.Api.Tests.Shared;

public class ExpiryRulesTests
{
    [Fact]
    public void NotBeExpired_Should_Return_False_When_Year_Is_In_Past()
    {
        // Arrange
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year - 1
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NotBeExpired_Should_Return_True_When_Year_Is_In_Future()
    {
        // Arrange
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year + 1
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NotBeExpired_Should_Return_True_When_Month_Is_Current_In_Current_Year()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NotBeExpired_Should_Return_False_When_Month_Is_Past_In_Current_Year()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var pastMonth = now.Month == 1 ? 12 : now.Month - 1;
        var year = now.Month == 1 ? now.Year - 1 : now.Year;
        
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = pastMonth,
            ExpiryYear = year
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NotBeExpired_Should_Return_True_When_Month_Is_Future_In_Current_Year()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var futureMonth = now.Month == 12 ? 1 : now.Month + 1;
        var year = now.Month == 12 ? now.Year + 1 : now.Year;
        
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = futureMonth,
            ExpiryYear = year
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(25)]
    public void NotBeExpired_Should_Return_True_When_Month_Is_Invalid(int invalidMonth)
    {
        // Arrange
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = invalidMonth,
            ExpiryYear = DateTime.UtcNow.Year + 1
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue(); // Invalid months return true (validation should catch this elsewhere)
    }

    [Fact]
    public void NotBeExpired_Should_Handle_December_Current_Year()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 12,
            ExpiryYear = currentYear
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        if (DateTime.UtcNow.Month <= 12)
        {
            result.Should().BeTrue();
        }
    }

    [Fact]
    public void NotBeExpired_Should_Handle_January_Next_Year()
    {
        // Arrange
        var nextYear = DateTime.UtcNow.Year + 1;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 1,
            ExpiryYear = nextYear
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void NotBeExpired_Should_Return_True_For_All_Valid_Months_In_Future_Year(int month)
    {
        // Arrange
        var futureYear = DateTime.UtcNow.Year + 2;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = month,
            ExpiryYear = futureYear
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void NotBeExpired_Should_Return_False_For_All_Months_In_Past_Year(int month)
    {
        // Arrange
        var pastYear = DateTime.UtcNow.Year - 1;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = month,
            ExpiryYear = pastYear
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NotBeExpired_Should_Handle_Edge_Case_Last_Day_Of_Month()
    {
        // This test verifies that the rule doesn't depend on specific days
        // since card expiry is typically month-based
        
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue(); // Current month should always be valid
    }

    [Fact]
    public void NotBeExpired_Should_Be_Consistent_For_Same_Input()
    {
        // Arrange
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1
        };

        // Act
        var result1 = ExpiryRules.NotBeExpired(dto);
        var result2 = ExpiryRules.NotBeExpired(dto);

        // Assert
        result1.Should().Be(result2);
        result1.Should().BeTrue();
    }

    [Fact]
    public void NotBeExpired_Should_Handle_Minimum_Valid_Future_Date()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var nextMonth = now.Month == 12 ? 1 : now.Month + 1;
        var nextYear = now.Month == 12 ? now.Year + 1 : now.Year;
        
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = nextMonth,
            ExpiryYear = nextYear
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NotBeExpired_Should_Handle_Maximum_Reasonable_Future_Date()
    {
        // Arrange
        var dto = new PaymentCommandDto
        {
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 10 // 10 years in future
        };

        // Act
        var result = ExpiryRules.NotBeExpired(dto);

        // Assert
        result.Should().BeTrue();
    }
}
using FluentValidation.TestHelper;
using PaymentGateway.Application.Queries;
using FluentAssertions;

namespace PaymentGateway.Api.Tests.GetPayment;

public class GetPaymentValidatorTests
{
    private readonly GetPaymentValidator _validator;

    public GetPaymentValidatorTests()
    {
        _validator = new GetPaymentValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        // Arrange
        var command = new GetPaymentCommand(Guid.Empty);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        // Arrange
        var command = new GetPaymentCommand(Guid.NewGuid());

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Not_Have_Any_Errors_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new GetPaymentCommand(Guid.NewGuid());

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
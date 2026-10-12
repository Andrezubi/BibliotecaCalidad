using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class CopyValidatorTests
{
    [Fact]
    public void Validate_ValidCopy_ShouldNotThrow()
    {
        // Arrange
        var copy = CreateValidCopy();

        // Act & Assert
        var action = () => CopyValidator.Validate(copy);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullCopy_ShouldThrowArgumentNullException()
    {
        // Arrange
        Copy? copy = null;

        // Act
        var action = () => CopyValidator.Validate(copy!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("copy");
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ZeroBookId_ShouldThrow(uint bookId)
    {
        // Arrange
        var copy = CreateValidCopy();
        copy.BookId = bookId;

        // Act
        var action = () => CopyValidator.Validate(copy);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("BookId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyInternalCode_ShouldThrow(string? internalCode)
    {
        // Arrange
        var copy = CreateValidCopy();
        copy.InternalCode = internalCode!;

        // Act
        var action = () => CopyValidator.Validate(copy);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("InternalCode"));
    }

    [Fact]
    public void Validate_InternalCodeTooLong_ShouldThrow()
    {
        // Arrange
        var copy = CreateValidCopy();
        copy.InternalCode = new string('a', 51);

        // Act
        var action = () => CopyValidator.Validate(copy);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("InternalCode"));
    }

    [Fact]
    public void Validate_InvalidInternalCodeCharacters_ShouldThrow()
    {
        // Arrange
        var copy = CreateValidCopy();
        copy.InternalCode = "INVALID@CODE";

        // Act
        var action = () => CopyValidator.Validate(copy);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("InternalCode"));
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ZeroStatusId_ShouldThrow(byte statusId)
    {
        // Arrange
        var copy = CreateValidCopy();
        copy.StatusId = statusId;

        // Act
        var action = () => CopyValidator.Validate(copy);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("StatusId"));
    }

    private static Copy CreateValidCopy()
    {
        return new Copy
        {
            BookId = 1,
            InternalCode = "ABC123",
            StatusId = 1,
            IsActive = true
        };
    }
}
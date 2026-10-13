using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class AuthorValidatorTests
{
    [Fact]
    public void Validate_ValidAuthor_ShouldNotThrow()
    {
        // Arrange
        var author = new Author
        {
            FirstName = "John",
            LastName = "Doe",
            SecondLastName = "Smith",
            IsActive = true
        };

        // Act & Assert
        var action = () => AuthorValidator.Validate(author);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullAuthor_ShouldThrowArgumentNullException()
    {
        // Arrange
        Author? author = null;

        // Act
        var action = () => AuthorValidator.Validate(author!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("author");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyFirstName_ShouldThrow(string? firstName)
    {
        // Arrange
        var author = CreateValidAuthor();
        author.FirstName = firstName!;

        // Act
        var action = () => AuthorValidator.Validate(author);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("FirstName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyLastName_ShouldThrow(string? lastName)
    {
        // Arrange
        var author = CreateValidAuthor();
        author.LastName = lastName!;

        // Act
        var action = () => AuthorValidator.Validate(author);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("LastName"));
    }

    [Fact]
    public void Validate_FirstNameTooLong_ShouldThrow()
    {
        // Arrange
        var author = CreateValidAuthor();
        author.FirstName = new string('a', 51);

        // Act
        var action = () => AuthorValidator.Validate(author);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("FirstName"));
    }

    [Fact]
    public void Validate_SecondLastNameTooLong_ShouldThrow()
    {
        // Arrange
        var author = CreateValidAuthor();
        author.SecondLastName = new string('a', 51);

        // Act
        var action = () => AuthorValidator.Validate(author);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("SecondLastName"));
    }

    private static Author CreateValidAuthor()
    {
        return new Author
        {
            FirstName = "John",
            LastName = "Doe",
            SecondLastName = "Smith",
            IsActive = true
        };
    }
}
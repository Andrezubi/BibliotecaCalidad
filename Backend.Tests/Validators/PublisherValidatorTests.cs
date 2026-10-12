using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class PublisherValidatorTests
{
    [Fact]
    public void Validate_ValidPublisher_ShouldNotThrow()
    {
        // Arrange
        var publisher = new Publisher
        {
            Name = "Test Publisher",
        };

        // Act & Assert
        var action = () => PublisherValidator.Validate(publisher);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullPublisher_ShouldThrowArgumentNullException()
    {
        // Arrange
        Publisher? publisher = null;

        // Act
        var action = () => PublisherValidator.Validate(publisher!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("publisher");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_ShouldThrow(string? name)
    {
        // Arrange
        var publisher = CreateValidPublisher();
        publisher.Name = name!;

        // Act
        var action = () => PublisherValidator.Validate(publisher);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Name"));
    }

    [Fact]
    public void Validate_NameTooLong_ShouldThrow()
    {
        // Arrange
        var publisher = CreateValidPublisher();
        publisher.Name = new string('a', 151);

        // Act
        var action = () => PublisherValidator.Validate(publisher);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Name"));
    }

    private static Publisher CreateValidPublisher()
    {
        return new Publisher
        {
            Name = "Test Publisher",
        };
    }
}
using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class CategoryValidatorTests
{
    [Fact]
    public void Validate_ValidCategory_ShouldNotThrow()
    {
        // Arrange
        var category = new Category
        {
            Name = "Fiction",
            Description = "Fiction books",
            IsActive = true
        };

        // Act & Assert
        var action = () => CategoryValidator.Validate(category);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullCategory_ShouldThrowArgumentNullException()
    {
        // Arrange
        Category? category = null;

        // Act
        var action = () => CategoryValidator.Validate(category!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("category");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_ShouldThrow(string? name)
    {
        // Arrange
        var category = CreateValidCategory();
        category.Name = name!;

        // Act
        var action = () => CategoryValidator.Validate(category);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Name"));
    }

    [Fact]
    public void Validate_NameTooLong_ShouldThrow()
    {
        // Arrange
        var category = CreateValidCategory();
        category.Name = new string('a', 51);

        // Act
        var action = () => CategoryValidator.Validate(category);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Name"));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldThrow()
    {
        // Arrange
        var category = CreateValidCategory();
        category.Description = new string('a', 501);

        // Act
        var action = () => CategoryValidator.Validate(category);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Description"));
    }

    private static Category CreateValidCategory()
    {
        return new Category
        {
            Name = "Fiction",
            Description = "Fiction books",
            IsActive = true
        };
    }
}
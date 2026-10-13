using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class BookValidatorTests
{
    [Fact]
    public void Validate_ValidBook_ShouldNotThrow()
    {
        // Arrange
        var book = new Book
        {
            Title = "Test Book",
            EditionNumber = 1,
            Isbn = "9781234567890",
            PublicationYear = 2024,
            PublisherId = 1,
            PageCount = 300,
            Description = "A test book",
            IsActive = true
        };

        // Act & Assert
        var action = () => BookValidator.Validate(book);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullBook_ShouldThrowArgumentNullException()
    {
        // Arrange
        Book? book = null;

        // Act
        var action = () => BookValidator.Validate(book!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("book");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTitle_ShouldThrow(string? title)
    {
        // Arrange
        var book = CreateValidBook();
        book.Title = title!;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Title"));
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.Title = new string('a', 256);

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Title"));
    }

    [Fact]
    public void Validate_ZeroEditionNumber_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.EditionNumber = 0;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("EditionNumber"));
    }

    [Theory]
    [InlineData("invalid-isbn")]
    [InlineData("123")]
    [InlineData("978123456789")] // 12 digits
    [InlineData("97812345678901")] // 14 digits
    public void Validate_InvalidISBN_ShouldThrow(string isbn)
    {
        // Arrange
        var book = CreateValidBook();
        book.Isbn = isbn;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("ISBN"));
    }

    [Fact]
    public void Validate_ValidISBN13_ShouldNotThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.Isbn = "9781234567890";

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(999)]
    [InlineData(2027)]
    public void Validate_InvalidPublicationYear_ShouldThrow(int year)
    {
        // Arrange
        var book = CreateValidBook();
        book.PublicationYear = (ushort)year;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("PublicationYear"));
    }

    [Fact]
    public void Validate_ZeroPublisherId_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.PublisherId = 0;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("PublisherId"));
    }

    [Fact]
    public void Validate_ZeroPageCount_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.PageCount = 0;

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("PageCount"));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.Description = new string('a', 401);

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Description"));
    }

    [Fact]
    public void Validate_InvalidCoverImageExtension_ShouldThrow()
    {
        // Arrange
        var book = CreateValidBook();
        book.CoverImage = "/uploads/books/test.pdf";

        // Act
        var action = () => BookValidator.Validate(book);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("CoverImage"));
    }

    private static Book CreateValidBook()
    {
        return new Book
        {
            Title = "Test Book",
            EditionNumber = 1,
            Isbn = "9781234567890",
            PublicationYear = 2024,
            PublisherId = 1,
            PageCount = 300,
            Description = "A test book",
            IsActive = true
        };
    }
}
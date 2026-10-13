using Backend.Domain.Validators;
using Backend.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Validators;

public class UserValidatorTests
{
    [Fact]
    public void Validate_ValidUser_ShouldNotThrow()
    {
        // Arrange
        var user = CreateValidUser();

        // Act & Assert
        var action = () => UserValidator.Validate(user);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullUser_ShouldThrowArgumentNullException()
    {
        // Arrange
        User? user = null;

        // Act
        var action = () => UserValidator.Validate(user!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("user");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidCi_ShouldThrow(int ci)
    {
        // Arrange
        var user = CreateValidUser();
        user.Ci = ci;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Ci"));
    }

    [Fact]
    public void Validate_ComplementTooLong_ShouldThrow()
    {
        // Arrange
        var user = CreateValidUser();
        user.Complement = "ABC";

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Complement"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyFirstName_ShouldThrow(string? firstName)
    {
        // Arrange
        var user = CreateValidUser();
        user.FirstName = firstName!;

        // Act
        var action = () => UserValidator.Validate(user);

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
        var user = CreateValidUser();
        user.LastName = lastName!;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("LastName"));
    }

    [Fact]
    public void Validate_InvalidPhone_ShouldThrow()
    {
        // Arrange
        var user = CreateValidUser();
        user.Phone = "invalid-phone";

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Phone"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyUsername_ShouldThrow(string? username)
    {
        // Arrange
        var user = CreateValidUser();
        user.Username = username!;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Username"));
    }

    [Fact]
    public void Validate_UsernameTooLong_ShouldThrow()
    {
        // Arrange
        var user = CreateValidUser();
        user.Username = new string('a', 16);

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Username"));
    }

    [Fact]
    public void Validate_InvalidUsernameCharacters_ShouldThrow()
    {
        // Arrange
        var user = CreateValidUser();
        user.Username = "user@name";

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Username"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyPasswordHash_ShouldThrow(string? passwordHash)
    {
        // Arrange
        var user = CreateValidUser();
        user.PasswordHash = passwordHash!;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("PasswordHash"));
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_InvalidRole_ShouldThrow(string? role)
    {
        // Arrange
        var user = CreateValidUser();
        user.Role = role!;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Role"));
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_InvalidStatus_ShouldThrow(string? status)
    {
        // Arrange
        var user = CreateValidUser();
        user.Status = status!;

        // Act
        var action = () => UserValidator.Validate(user);

        // Assert
        action.Should().Throw<BookValidationException>()
            .Where(e => e.Errors.ContainsKey("Status"));
    }

    private static User CreateValidUser()
    {
        return new User
        {
            Ci = 12345678,
            Complement = "A",
            FirstName = "John",
            LastName = "Doe",
            SecondLastName = "Smith",
            Phone = "123456789",
            Username = "johndoe",
            PasswordHash = "hashedpassword",
            Role = "User",
            Status = "Active",
            IsActive = true
        };
    }
}
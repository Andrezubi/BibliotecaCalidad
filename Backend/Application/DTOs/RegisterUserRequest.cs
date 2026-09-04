namespace Backend.Application.DTOs;

public class RegisterUserRequest
{
    public int Ci { get; set; }

    public string? Complement { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
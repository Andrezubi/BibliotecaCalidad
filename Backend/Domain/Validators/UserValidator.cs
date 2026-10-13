using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class UserValidator
{
    private static readonly string[] ValidStatuses = { "Active", "Blocked", "Inactive" };
    private static readonly string[] ValidRoles = { "Admin", "Librarian", "User" };

    public static void Validate(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "Ci", () => ValidateCi(user.Ci));
        AddValidation(errors, "Complement", () => ValidateComplement(user.Complement));
        AddValidation(errors, "FirstName", () => ValidateFirstName(user.FirstName));
        AddValidation(errors, "LastName", () => ValidateLastName(user.LastName));
        AddValidation(errors, "SecondLastName", () => ValidateSecondLastName(user.SecondLastName));
        AddValidation(errors, "Phone", () => ValidatePhone(user.Phone));
        AddValidation(errors, "Username", () => ValidateUsername(user.Username));
        AddValidation(errors, "PasswordHash", () => ValidatePasswordHash(user.PasswordHash));
        AddValidation(errors, "Role", () => ValidateRole(user.Role));
        AddValidation(errors, "Status", () => ValidateStatus(user.Status));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateCi(int ci)
    {
        if (ci <= 0)
        {
            throw new ArgumentException("El CI debe ser un número positivo.");
        }
    }

    private static void ValidateComplement(string? complement)
    {
        if (string.IsNullOrWhiteSpace(complement))
        {
            return;
        }

        if (complement.Length > 2)
        {
            throw new ArgumentException("El complemento no puede superar los 2 caracteres.");
        }
    }

    private static void ValidateFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        var normalized = NormalizeSpaces(firstName);

        if (normalized.Length > 50)
        {
            throw new ArgumentException("El nombre no puede superar los 50 caracteres.");
        }

        if (!string.Equals(firstName, normalized, StringComparison.Ordinal))
        {
            throw new ArgumentException("El nombre no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalized))
        {
            throw new ArgumentException("El nombre contiene caracteres no válidos.");
        }
    }

    private static void ValidateLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("El apellido es obligatorio.");
        }

        var normalized = NormalizeSpaces(lastName);

        if (normalized.Length > 50)
        {
            throw new ArgumentException("El apellido no puede superar los 50 caracteres.");
        }

        if (!string.Equals(lastName, normalized, StringComparison.Ordinal))
        {
            throw new ArgumentException("El apellido no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalized))
        {
            throw new ArgumentException("El apellido contiene caracteres no válidos.");
        }
    }

    private static void ValidateSecondLastName(string? secondLastName)
    {
        if (string.IsNullOrWhiteSpace(secondLastName))
        {
            return;
        }

        var normalized = NormalizeSpaces(secondLastName);

        if (normalized.Length > 50)
        {
            throw new ArgumentException("El segundo apellido no puede superar los 50 caracteres.");
        }

        if (!string.Equals(secondLastName, normalized, StringComparison.Ordinal))
        {
            throw new ArgumentException("El segundo apellido no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalized))
        {
            throw new ArgumentException("El segundo apellido contiene caracteres no válidos.");
        }
    }

    private static void ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return;
        }

        if (phone.Length > 15)
        {
            throw new ArgumentException("El teléfono no puede superar los 15 caracteres.");
        }

        if (!Regex.IsMatch(phone, @"^[\d\s\-\+\(\)]+$"))
        {
            throw new ArgumentException("El teléfono contiene caracteres no válidos.");
        }
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        }

        if (username.Length > 15)
        {
            throw new ArgumentException("El nombre de usuario no puede superar los 15 caracteres.");
        }

        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_\.]+$"))
        {
            throw new ArgumentException("El nombre de usuario solo puede contener letras, números, guiones bajos y puntos.");
        }
    }

    private static void ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("El hash de la contraseña es obligatorio.");
        }

        if (passwordHash.Length > 100)
        {
            throw new ArgumentException("El hash de la contraseña no puede superar los 100 caracteres.");
        }
    }

    private static void ValidateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("El rol es obligatorio.");
        }

        if (!ValidRoles.Contains(role))
        {
            throw new ArgumentException($"El rol debe ser uno de: {string.Join(", ", ValidRoles)}.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("El estado es obligatorio.");
        }

        if (!ValidStatuses.Contains(status))
        {
            throw new ArgumentException($"El estado debe ser uno de: {string.Join(", ", ValidStatuses)}.");
        }
    }

    private static void AddValidation(
        Dictionary<string, List<string>> errors,
        string property,
        Action validation)
    {
        try
        {
            validation();
        }
        catch (ArgumentException ex)
        {
            if (!errors.ContainsKey(property))
            {
                errors[property] = new List<string>();
            }

            errors[property].Add(ex.Message);
        }
    }

    private static void ThrowIfHasErrors(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new BookValidationException(
            errors.ToDictionary(
                x => x.Key,
                x => x.Value.ToArray()));
    }

    private static string NormalizeSpaces(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static bool ContainsValidText(string value)
    {
        return value.All(c =>
            char.IsLetterOrDigit(c) ||
            char.IsWhiteSpace(c) ||
            char.IsPunctuation(c));
    }
}
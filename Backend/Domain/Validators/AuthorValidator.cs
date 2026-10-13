using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class AuthorValidator
{
    public static void Validate(Author author)
    {
        if (author is null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(
            errors,
            "FirstName",
            () => ValidateFirstName(author.FirstName));

        AddValidation(
            errors,
            "LastName",
            () => ValidateLastName(author.LastName));

        AddValidation(
            errors,
            "SecondLastName",
            () => ValidateSecondLastName(author.SecondLastName));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("El nombre del autor es obligatorio.");
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
            throw new ArgumentException("El apellido del autor es obligatorio.");
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
using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class CategoryValidator
{
    public static void Validate(Category category)
    {
        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(
            errors,
            "Name",
            () => ValidateName(category.Name));

        AddValidation(
            errors,
            "Description",
            () => ValidateDescription(category.Description));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre de la categoría es obligatorio.");
        }

        var normalized = NormalizeSpaces(name);

        if (normalized.Length > 50)
        {
            throw new ArgumentException("El nombre no puede superar los 50 caracteres.");
        }

        if (!string.Equals(name, normalized, StringComparison.Ordinal))
        {
            throw new ArgumentException("El nombre no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalized))
        {
            throw new ArgumentException("El nombre contiene caracteres no válidos.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var normalized = NormalizeSpaces(description);

        if (normalized.Length > 500)
        {
            throw new ArgumentException("La descripción no puede superar los 500 caracteres.");
        }

        if (!string.Equals(description, normalized, StringComparison.Ordinal))
        {
            throw new ArgumentException("La descripción no debe contener espacios innecesarios.");
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
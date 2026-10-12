using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class PublisherValidator
{
    public static void Validate(Publisher publisher)
    {
        if (publisher is null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(
            errors,
            "Name",
            () => ValidateName(publisher.Name));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre de la editorial es obligatorio.");
        }

        var normalized = NormalizeSpaces(name);

        if (normalized.Length > 150)
        {
            throw new ArgumentException("El nombre no puede superar los 150 caracteres.");
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
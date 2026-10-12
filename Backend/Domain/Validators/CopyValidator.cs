using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class CopyValidator
{
    public static void Validate(Copy copy)
    {
        if (copy is null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "BookId", () => ValidateBookId(copy.BookId));
        AddValidation(errors, "InternalCode", () => ValidateInternalCode(copy.InternalCode));
        AddValidation(errors, "StatusId", () => ValidateStatusId(copy.StatusId));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateBookId(uint bookId)
    {
        if (bookId == 0)
        {
            throw new ArgumentException("El ID del libro es obligatorio.");
        }
    }

    private static void ValidateInternalCode(string internalCode)
    {
        if (string.IsNullOrWhiteSpace(internalCode))
        {
            throw new ArgumentException("El código interno es obligatorio.");
        }

        if (internalCode.Length > 50)
        {
            throw new ArgumentException("El código interno no puede superar los 50 caracteres.");
        }

        if (!Regex.IsMatch(internalCode, @"^[a-zA-Z0-9\-_]+$"))
        {
            throw new ArgumentException("El código interno solo puede contener letras, números, guiones y guiones bajos.");
        }
    }

    private static void ValidateStatusId(byte statusId)
    {
        if (statusId == 0)
        {
            throw new ArgumentException("El estado es obligatorio.");
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
}
using Backend.Models;

namespace Backend.Domain.Validators;

public static class CopyStatusValidator
{
    public static void Validate(Copystatus copyStatus)
    {
        if (copyStatus is null)
        {
            throw new ArgumentNullException(nameof(copyStatus));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "Name", () => ValidateName(copyStatus.Name));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del estado es obligatorio.");
        }

        if (name.Length > 30)
        {
            throw new ArgumentException("El nombre no puede superar los 30 caracteres.");
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
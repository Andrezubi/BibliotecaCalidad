using Backend.Models;

namespace Backend.Domain.Validators;

public static class LoanRequestItemValidator
{
    public static void Validate(Loanrequestitem loanRequestItem)
    {
        if (loanRequestItem is null)
        {
            throw new ArgumentNullException(nameof(loanRequestItem));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "LoanRequestId", () => ValidateLoanRequestId(loanRequestItem.LoanRequestId));
        AddValidation(errors, "CopyId", () => ValidateCopyId(loanRequestItem.CopyId));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateLoanRequestId(uint loanRequestId)
    {
        if (loanRequestId == 0)
        {
            throw new ArgumentException("El ID de la solicitud de préstamo es obligatorio.");
        }
    }

    private static void ValidateCopyId(uint copyId)
    {
        if (copyId == 0)
        {
            throw new ArgumentException("El ID de la copia es obligatorio.");
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
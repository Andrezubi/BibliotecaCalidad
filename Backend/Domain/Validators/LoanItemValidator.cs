using Backend.Models;

namespace Backend.Domain.Validators;

public static class LoanItemValidator
{
    private static readonly string[] ValidStatuses = { "Active", "Returned", "Overdue", "Lost" };

    public static void Validate(Loanitem loanItem)
    {
        if (loanItem is null)
        {
            throw new ArgumentNullException(nameof(loanItem));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "LoanId", () => ValidateLoanId(loanItem.LoanId));
        AddValidation(errors, "CopyId", () => ValidateCopyId(loanItem.CopyId));
        AddValidation(errors, "Status", () => ValidateStatus(loanItem.Status));
        AddValidation(errors, "ReturnedAt", () => ValidateReturnedAt(loanItem.ReturnedAt, loanItem.Status));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateLoanId(uint loanId)
    {
        if (loanId == 0)
        {
            throw new ArgumentException("El ID del préstamo es obligatorio.");
        }
    }

    private static void ValidateCopyId(uint copyId)
    {
        if (copyId == 0)
        {
            throw new ArgumentException("El ID de la copia es obligatorio.");
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

    private static void ValidateReturnedAt(DateTime? returnedAt, string status)
    {
        if (status == "Returned" && !returnedAt.HasValue)
        {
            throw new ArgumentException("La fecha de devolución es obligatoria cuando el estado es 'Returned'.");
        }

        if (returnedAt.HasValue && returnedAt.Value > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de devolución no puede ser futura.");
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
using Backend.Models;

namespace Backend.Domain.Validators;

public static class FineValidator
{
    private static readonly string[] ValidStatuses = { "Pending", "PartiallyPaid", "Paid", "Waived" };

    public static void Validate(Fine fine)
    {
        if (fine is null)
        {
            throw new ArgumentNullException(nameof(fine));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "LoanId", () => ValidateLoanId(fine.LoanId));
        AddValidation(errors, "CopyId", () => ValidateCopyId(fine.CopyId));
        AddValidation(errors, "DailyRate", () => ValidateDailyRate(fine.DailyRate));
        AddValidation(errors, "OverdueDays", () => ValidateOverdueDays(fine.OverdueDays));
        AddValidation(errors, "Amount", () => ValidateAmount(fine.Amount));
        AddValidation(errors, "Status", () => ValidateStatus(fine.Status));

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

    private static void ValidateDailyRate(decimal dailyRate)
    {
        if (dailyRate < 0)
        {
            throw new ArgumentException("La tarifa diaria no puede ser negativa.");
        }
    }

    private static void ValidateOverdueDays(uint overdueDays)
    {
        // OverdueDays can be 0
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("El monto no puede ser negativo.");
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
}
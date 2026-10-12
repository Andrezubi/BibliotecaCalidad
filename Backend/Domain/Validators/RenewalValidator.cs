using Backend.Models;

namespace Backend.Domain.Validators;

public static class RenewalValidator
{
    public static void Validate(Renewal renewal)
    {
        if (renewal is null)
        {
            throw new ArgumentNullException(nameof(renewal));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "LoanId", () => ValidateLoanId(renewal.LoanId));
        AddValidation(errors, "CopyId", () => ValidateCopyId(renewal.CopyId));
        AddValidation(errors, "LibrarianId", () => ValidateLibrarianId(renewal.LibrarianId));
        AddValidation(errors, "RenewalDate", () => ValidateRenewalDate(renewal.RenewalDate));
        AddValidation(errors, "PreviousDueDate", () => ValidatePreviousDueDate(renewal.PreviousDueDate));
        AddValidation(errors, "NewDueDate", () => ValidateNewDueDate(renewal.NewDueDate, renewal.PreviousDueDate));

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

    private static void ValidateLibrarianId(ushort librarianId)
    {
        if (librarianId == 0)
        {
            throw new ArgumentException("El ID del bibliotecario es obligatorio.");
        }
    }

    private static void ValidateRenewalDate(DateTime renewalDate)
    {
        if (renewalDate == default)
        {
            throw new ArgumentException("La fecha de renovación es obligatoria.");
        }

        if (renewalDate > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de renovación no puede ser futura.");
        }
    }

    private static void ValidatePreviousDueDate(DateTime previousDueDate)
    {
        if (previousDueDate == default)
        {
            throw new ArgumentException("La fecha de vencimiento anterior es obligatoria.");
        }
    }

    private static void ValidateNewDueDate(DateTime newDueDate, DateTime previousDueDate)
    {
        if (newDueDate == default)
        {
            throw new ArgumentException("La nueva fecha de vencimiento es obligatoria.");
        }

        if (newDueDate <= previousDueDate)
        {
            throw new ArgumentException("La nueva fecha de vencimiento debe ser posterior a la anterior.");
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
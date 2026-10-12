using Backend.Models;

namespace Backend.Domain.Validators;

public static class LoanValidator
{
    private static readonly string[] ValidStatuses = { "Active", "Completed", "Overdue", "Lost" };

    public static void Validate(Loan loan)
    {
        if (loan is null)
        {
            throw new ArgumentNullException(nameof(loan));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "BorrowerId", () => ValidateBorrowerId(loan.BorrowerId));
        AddValidation(errors, "LibrarianId", () => ValidateLibrarianId(loan.LibrarianId));
        AddValidation(errors, "LoanDate", () => ValidateLoanDate(loan.LoanDate));
        AddValidation(errors, "DueDate", () => ValidateDueDate(loan.DueDate, loan.LoanDate));
        AddValidation(errors, "Status", () => ValidateStatus(loan.Status));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateBorrowerId(ushort borrowerId)
    {
        if (borrowerId == 0)
        {
            throw new ArgumentException("El ID del prestatario es obligatorio.");
        }
    }

    private static void ValidateLibrarianId(ushort librarianId)
    {
        if (librarianId == 0)
        {
            throw new ArgumentException("El ID del bibliotecario es obligatorio.");
        }
    }

    private static void ValidateLoanDate(DateTime loanDate)
    {
        if (loanDate == default)
        {
            throw new ArgumentException("La fecha de préstamo es obligatoria.");
        }

        if (loanDate > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de préstamo no puede ser futura.");
        }
    }

    private static void ValidateDueDate(DateTime dueDate, DateTime loanDate)
    {
        if (dueDate == default)
        {
            throw new ArgumentException("La fecha de vencimiento es obligatoria.");
        }

        if (dueDate <= loanDate)
        {
            throw new ArgumentException("La fecha de vencimiento debe ser posterior a la fecha de préstamo.");
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
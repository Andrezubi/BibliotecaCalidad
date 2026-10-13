using Backend.Models;

namespace Backend.Domain.Validators;

public static class LoanRequestValidator
{
    private static readonly string[] ValidStatuses = { "Pending", "Approved", "Rejected", "Completed", "Cancelled" };

    public static void Validate(Loanrequest loanRequest)
    {
        if (loanRequest is null)
        {
            throw new ArgumentNullException(nameof(loanRequest));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "ReaderId", () => ValidateReaderId(loanRequest.ReaderId));
        AddValidation(errors, "RequestedAt", () => ValidateRequestedAt(loanRequest.RequestedAt));
        AddValidation(errors, "Status", () => ValidateStatus(loanRequest.Status));
        AddValidation(errors, "ProcessedAt", () => ValidateProcessedAt(loanRequest.ProcessedAt, loanRequest.Status));
        AddValidation(errors, "ProcessedBy", () => ValidateProcessedBy(loanRequest.ProcessedBy, loanRequest.Status));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateReaderId(ushort readerId)
    {
        if (readerId == 0)
        {
            throw new ArgumentException("El ID del lector es obligatorio.");
        }
    }

    private static void ValidateRequestedAt(DateTime requestedAt)
    {
        if (requestedAt == default)
        {
            throw new ArgumentException("La fecha de solicitud es obligatoria.");
        }

        if (requestedAt > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de solicitud no puede ser futura.");
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

    private static void ValidateProcessedAt(DateTime? processedAt, string status)
    {
        if ((status == "Approved" || status == "Rejected" || status == "Completed") && !processedAt.HasValue)
        {
            throw new ArgumentException("La fecha de procesamiento es obligatoria cuando el estado indica procesamiento.");
        }

        if (processedAt.HasValue && processedAt.Value > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de procesamiento no puede ser futura.");
        }
    }

    private static void ValidateProcessedBy(ushort? processedBy, string status)
    {
        if ((status == "Approved" || status == "Rejected" || status == "Completed") && !processedBy.HasValue)
        {
            throw new ArgumentException("El ID del procesador es obligatorio cuando el estado indica procesamiento.");
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
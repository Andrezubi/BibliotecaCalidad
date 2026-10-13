using Backend.Models;

namespace Backend.Domain.Validators;

public static class ReservationValidator
{
    private static readonly string[] ValidStatuses = { "Pending", "Active", "Completed", "Cancelled", "Expired" };

    public static void Validate(Reservation reservation)
    {
        if (reservation is null)
        {
            throw new ArgumentNullException(nameof(reservation));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "ReaderId", () => ValidateReaderId(reservation.ReaderId));
        AddValidation(errors, "CopyId", () => ValidateCopyId(reservation.CopyId));
        AddValidation(errors, "StartAt", () => ValidateStartAt(reservation.StartAt));
        AddValidation(errors, "EndAt", () => ValidateEndAt(reservation.EndAt, reservation.StartAt));
        AddValidation(errors, "Status", () => ValidateStatus(reservation.Status));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateReaderId(ushort readerId)
    {
        if (readerId == 0)
        {
            throw new ArgumentException("El ID del lector es obligatorio.");
        }
    }

    private static void ValidateCopyId(uint copyId)
    {
        if (copyId == 0)
        {
            throw new ArgumentException("El ID de la copia es obligatorio.");
        }
    }

    private static void ValidateStartAt(DateTime startAt)
    {
        if (startAt == default)
        {
            throw new ArgumentException("La fecha de inicio es obligatoria.");
        }
    }

    private static void ValidateEndAt(DateTime endAt, DateTime startAt)
    {
        if (endAt == default)
        {
            throw new ArgumentException("La fecha de fin es obligatoria.");
        }

        if (endAt <= startAt)
        {
            throw new ArgumentException("La fecha de fin debe ser posterior a la fecha de inicio.");
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
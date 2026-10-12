using Backend.Models;

namespace Backend.Domain.Validators;

public static class FinePaymentValidator
{
    public static void Validate(Finepayment finePayment)
    {
        if (finePayment is null)
        {
            throw new ArgumentNullException(nameof(finePayment));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "FineId", () => ValidateFineId(finePayment.FineId));
        AddValidation(errors, "LibrarianId", () => ValidateLibrarianId(finePayment.LibrarianId));
        AddValidation(errors, "Amount", () => ValidateAmount(finePayment.Amount));
        AddValidation(errors, "ReceiptNumber", () => ValidateReceiptNumber(finePayment.ReceiptNumber));
        AddValidation(errors, "PaidAt", () => ValidatePaidAt(finePayment.PaidAt));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateFineId(uint fineId)
    {
        if (fineId == 0)
        {
            throw new ArgumentException("El ID de la multa es obligatorio.");
        }
    }

    private static void ValidateLibrarianId(ushort librarianId)
    {
        if (librarianId == 0)
        {
            throw new ArgumentException("El ID del bibliotecario es obligatorio.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("El monto debe ser mayor a cero.");
        }
    }

    private static void ValidateReceiptNumber(string receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            throw new ArgumentException("El número de recibo es obligatorio.");
        }

        if (receiptNumber.Length > 50)
        {
            throw new ArgumentException("El número de recibo no puede superar los 50 caracteres.");
        }
    }

    private static void ValidatePaidAt(DateTime paidAt)
    {
        if (paidAt == default)
        {
            throw new ArgumentException("La fecha de pago es obligatoria.");
        }

        if (paidAt > DateTime.Now.AddDays(1))
        {
            throw new ArgumentException("La fecha de pago no puede ser futura.");
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
using Backend.Models;

namespace Backend.Domain.Validators;

public static class NotificationValidator
{
    public static void Validate(Notification notification)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "RecipientId", () => ValidateRecipientId(notification.RecipientId));
        AddValidation(errors, "Type", () => ValidateType(notification.Type));
        AddValidation(errors, "Title", () => ValidateTitle(notification.Title));
        AddValidation(errors, "Message", () => ValidateMessage(notification.Message));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateRecipientId(ushort recipientId)
    {
        if (recipientId == 0)
        {
            throw new ArgumentException("El ID del destinatario es obligatorio.");
        }
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("El tipo de notificación es obligatorio.");
        }

        if (type.Length > 50)
        {
            throw new ArgumentException("El tipo no puede superar los 50 caracteres.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("El título es obligatorio.");
        }

        if (title.Length > 150)
        {
            throw new ArgumentException("El título no puede superar los 150 caracteres.");
        }
    }

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.");
        }

        if (message.Length > 500)
        {
            throw new ArgumentException("El mensaje no puede superar los 500 caracteres.");
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
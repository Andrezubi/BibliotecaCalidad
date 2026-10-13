using Backend.Models;

namespace Backend.Domain.Validators;

public static class BookAuthorValidator
{
    public static void Validate(Bookauthor bookAuthor)
    {
        if (bookAuthor is null)
        {
            throw new ArgumentNullException(nameof(bookAuthor));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "BookId", () => ValidateBookId(bookAuthor.BookId));
        AddValidation(errors, "AuthorId", () => ValidateAuthorId(bookAuthor.AuthorId));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateBookId(uint bookId)
    {
        if (bookId == 0)
        {
            throw new ArgumentException("El ID del libro es obligatorio.");
        }
    }

    private static void ValidateAuthorId(uint authorId)
    {
        if (authorId == 0)
        {
            throw new ArgumentException("El ID del autor es obligatorio.");
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
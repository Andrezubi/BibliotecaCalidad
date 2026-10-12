using Backend.Models;

namespace Backend.Domain.Validators;

public static class BookCategoryValidator
{
    public static void Validate(Bookcategory bookCategory)
    {
        if (bookCategory is null)
        {
            throw new ArgumentNullException(nameof(bookCategory));
        }

        var errors = new Dictionary<string, List<string>>();

        AddValidation(errors, "BookId", () => ValidateBookId(bookCategory.BookId));
        AddValidation(errors, "CategoryId", () => ValidateCategoryId(bookCategory.CategoryId));

        ThrowIfHasErrors(errors);
    }

    private static void ValidateBookId(uint bookId)
    {
        if (bookId == 0)
        {
            throw new ArgumentException("El ID del libro es obligatorio.");
        }
    }

    private static void ValidateCategoryId(byte categoryId)
    {
        if (categoryId == 0)
        {
            throw new ArgumentException("El ID de la categoría es obligatorio.");
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
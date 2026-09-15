using Backend.Domain.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class BookValidator
{
    private const int CurrentYear = 2026;

    private static readonly string[] AllowedCoverExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public static void Validate(Book book)
    {
        if (book is null)
        {
            throw new ArgumentNullException(nameof(book));
        }

        var errors = new Dictionary<string, List<string>>();

        // ============================================================
        // DATOS DEL LIBRO
        // ============================================================

        AddValidation(
            errors,
            "Title",
            () => ValidateTitle(book.Title));

        AddValidation(
            errors,
            "EditionNumber",
            () => ValidateEditionNumber(book.EditionNumber));

        AddValidation(
            errors,
            "ISBN",
            () => ValidateISBN(book.ISBN));

        AddValidation(
            errors,
            "PublicationYear",
            () => ValidatePublicationYear(book.PublicationYear));

        AddValidation(
            errors,
            "Publisher",
            () => ValidatePublisher(book.Publisher));

        AddValidation(
            errors,
            "PageCount",
            () => ValidatePageCount(book.PageCount));

        AddValidation(
            errors,
            "Description",
            () => ValidateDescription(book.Description));

        AddValidation(
            errors,
            "CoverImage",
            () => ValidateCoverPath(book.CoverImage));

        // ============================================================
        // RESULTADO
        // ============================================================

        ThrowIfHasErrors(errors);
    }

    // ============================================================
    // VALIDACIÓN DE AUTORES
    // ============================================================

    public static void ValidateAuthorIds(
        IEnumerable<int>? authorIds)
    {
        var errors = new Dictionary<string, List<string>>();

        AddValidation(
            errors,
            "AuthorIds",
            () =>
            {
                if (authorIds == null)
                {
                    throw new ArgumentException(
                        "Debe seleccionar al menos un autor.");
                }

                var ids = authorIds
                    .Where(id => id > 0)
                    .ToList();

                if (!ids.Any())
                {
                    throw new ArgumentException(
                        "Debe seleccionar al menos un autor.");
                }

                if (ids.Count != ids.Distinct().Count())
                {
                    throw new ArgumentException(
                        "No se puede seleccionar el mismo autor más de una vez.");
                }
            });

        ThrowIfHasErrors(errors);
    }

    // ============================================================
    // VALIDACIÓN DE CATEGORÍAS
    // ============================================================

    public static void ValidateCategoryIds(
        IEnumerable<int>? categoryIds)
    {
        var errors = new Dictionary<string, List<string>>();

        AddValidation(
            errors,
            "CategoryIds",
            () =>
            {
                if (categoryIds == null)
                {
                    throw new ArgumentException(
                        "Debe seleccionar al menos una categoría.");
                }

                var ids = categoryIds
                    .Where(id => id > 0)
                    .ToList();

                if (!ids.Any())
                {
                    throw new ArgumentException(
                        "Debe seleccionar al menos una categoría.");
                }

                if (ids.Count != ids.Distinct().Count())
                {
                    throw new ArgumentException(
                        "No se puede seleccionar la misma categoría más de una vez.");
                }
            });

        ThrowIfHasErrors(errors);
    }

    // ============================================================
    // VALIDACIÓN DE PORTADA
    // ============================================================

    public static void ValidateCoverPath(
        string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(coverImage))
        {
            return;
        }

        var extension =
            Path.GetExtension(coverImage)
                .ToLowerInvariant();

        if (!AllowedCoverExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "La portada debe estar en formato JPG, JPEG, PNG o WEBP.");
        }
    }

    // ============================================================
    // TÍTULO
    // ============================================================

    private static void ValidateTitle(
        string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "El título es obligatorio.");
        }

        var normalizedTitle =
            NormalizeSpaces(title);

        if (normalizedTitle.Length > 50)
        {
            throw new ArgumentException(
                "El título no puede superar los 50 caracteres.");
        }

        if (!string.Equals(
                title,
                normalizedTitle,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "El título no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalizedTitle))
        {
            throw new ArgumentException(
                "El título contiene caracteres no válidos.");
        }
    }

    // ============================================================
    // NÚMERO DE EDICIÓN
    // ============================================================

    private static void ValidateEditionNumber(
        int? editionNumber)
    {
        if (!editionNumber.HasValue)
        {
            throw new ArgumentException(
                "El número de edición es obligatorio.");
        }

        if (editionNumber <= 0)
        {
            throw new ArgumentException(
                "El número de edición debe ser mayor a 0.");
        }
    }

    // ============================================================
    // ISBN
    // ============================================================

    private static void ValidateISBN(
        string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            throw new ArgumentException(
                "El ISBN es obligatorio.");
        }

        var trimmedIsbn =
            isbn.Trim();

        if (!string.Equals(
                trimmedIsbn,
                isbn,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "El ISBN no debe contener espacios al inicio o al final.");
        }

        if (!Regex.IsMatch(
                trimmedIsbn,
                @"^[0-9Xx\-\s]+$"))
        {
            throw new ArgumentException(
                "El ISBN contiene caracteres no válidos.");
        }

        var cleanIsbn =
            Regex.Replace(
                trimmedIsbn,
                @"[\s-]",
                "");

        if (cleanIsbn.Length != 13)
        {
            throw new ArgumentException(
                "El ISBN debe contener 13 dígitos.");
        }

        ValidateISBN13(cleanIsbn);
    }

    private static void ValidateISBN13(
        string isbn)
    {
        if (!isbn.All(char.IsDigit))
        {
            throw new ArgumentException(
                "El ISBN-13 debe contener únicamente 13 dígitos.");
        }
    }

    // ============================================================
    // AÑO DE PUBLICACIÓN
    // ============================================================

    private static void ValidatePublicationYear(
        int? publicationYear)
    {
        if (!publicationYear.HasValue)
        {
            throw new ArgumentException(
                "El año de publicación es obligatorio.");
        }

        if (publicationYear < 1000 ||
            publicationYear > CurrentYear)
        {
            throw new ArgumentException(
                $"El año de publicación debe estar entre 1000 y {CurrentYear}.");
        }
    }

    // ============================================================
    // EDITORIAL
    // ============================================================

    private static void ValidatePublisher(
        string? publisher)
    {
        if (string.IsNullOrWhiteSpace(publisher))
        {
            throw new ArgumentException(
                "La editorial es obligatoria.");
        }

        var normalizedPublisher =
            NormalizeSpaces(publisher);

        if (normalizedPublisher.Length > 50)
        {
            throw new ArgumentException(
                "La editorial no puede superar los 50 caracteres.");
        }

        if (!string.Equals(
                publisher,
                normalizedPublisher,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La editorial no debe contener espacios innecesarios.");
        }

        if (!ContainsValidText(normalizedPublisher))
        {
            throw new ArgumentException(
                "La editorial contiene caracteres no válidos.");
        }
    }

    // ============================================================
    // CANTIDAD DE PÁGINAS
    // ============================================================

    private static void ValidatePageCount(
        int? pageCount)
    {
        if (!pageCount.HasValue)
        {
            throw new ArgumentException(
                "La cantidad de páginas es obligatoria.");
        }

        if (pageCount <= 0)
        {
            throw new ArgumentException(
                "La cantidad de páginas debe ser mayor a 0.");
        }
    }

    // ============================================================
    // DESCRIPCIÓN
    // ============================================================

    private static void ValidateDescription(
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var normalizedDescription =
            NormalizeSpaces(description);

        if (normalizedDescription.Length > 500)
        {
            throw new ArgumentException(
                "La descripción no puede superar los 500 caracteres.");
        }

        if (!string.Equals(
                description,
                normalizedDescription,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La descripción no debe contener espacios innecesarios.");
        }
    }

    // ============================================================
    // ERRORES
    // ============================================================

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
                errors[property] =
                    new List<string>();
            }

            errors[property].Add(ex.Message);
        }
    }

    private static void ThrowIfHasErrors(
        Dictionary<string, List<string>> errors)
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

    // ============================================================
    // UTILIDADES
    // ============================================================

    private static string NormalizeSpaces(
        string value)
    {
        return Regex.Replace(
            value.Trim(),
            @"\s+",
            " ");
    }

    private static bool ContainsValidText(
        string value)
    {
        return value.All(c =>
            char.IsLetterOrDigit(c) ||
            char.IsWhiteSpace(c) ||
            char.IsPunctuation(c));
    }
}
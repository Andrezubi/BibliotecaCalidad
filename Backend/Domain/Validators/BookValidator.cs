using Backend.Domain.Models;
using System.Text.RegularExpressions;

namespace Backend.Domain.Validators;

public static class BookValidator
{
    private const int CurrentYear = 2026;

    public static void Validate(Book book)
    {
        if (book is null)
        {
            throw new ArgumentNullException(nameof(book));
        }

        ValidateTitle(book.Title);
        ValidateEditionNumber(book.EditionNumber);
        ValidateISBN(book.ISBN);
        ValidatePublicationYear(book.PublicationYear);
        ValidatePublisher(book.Publisher);
        ValidatePageCount(book.PageCount);
        ValidateDescription(book.Description);
    }

    // ============================================================
    // TÍTULO
    // ============================================================

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "El título del libro es obligatorio.");
        }

        var normalizedTitle = NormalizeSpaces(title);

        if (normalizedTitle.Length > 50)
        {
            throw new ArgumentException(
                "El título no puede superar los 50 caracteres.");
        }

        if (!string.Equals(title, normalizedTitle, StringComparison.Ordinal))
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

    private static void ValidateEditionNumber(int editionNumber)
    {
        if (editionNumber <= 0)
        {
            throw new ArgumentException(
                "El número de edición debe ser mayor a 0.");
        }
    }

    // ============================================================
    // ISBN
    // ============================================================

    private static void ValidateISBN(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return;
        }

        var trimmedIsbn = isbn.Trim();

        if (trimmedIsbn != isbn)
        {
            throw new ArgumentException(
                "El ISBN no debe contener espacios al inicio o al final.");
        }

        // Permitimos solamente:
        // - números
        // - guiones
        // - espacios internos
        // - X/x para ISBN-10
        if (!Regex.IsMatch(
                trimmedIsbn,
                @"^[0-9Xx\-\s]+$"))
        {
            throw new ArgumentException(
                "El ISBN contiene caracteres no válidos.");
        }

        // Eliminamos guiones y espacios para validar el ISBN real.
        var cleanIsbn = Regex.Replace(
            trimmedIsbn,
            @"[\s-]",
            "");

        // ISBN-10
        if (cleanIsbn.Length == 10)
        {
            ValidateISBN10(cleanIsbn);
            return;
        }

        // ISBN-13
        if (cleanIsbn.Length == 13)
        {
            ValidateISBN13(cleanIsbn);
            return;
        }

        throw new ArgumentException(
            "El ISBN debe contener 10 o 13 dígitos.");
    }

    private static void ValidateISBN10(string isbn)
    {
        // Los primeros 9 caracteres deben ser números.
        for (int i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                throw new ArgumentException(
                    "El ISBN-10 debe contener 9 dígitos y un dígito de control.");
            }
        }

        // El último carácter puede ser un número o X.
        if (!char.IsDigit(isbn[9]) &&
            isbn[9] != 'X' &&
            isbn[9] != 'x')
        {
            throw new ArgumentException(
                "El dígito de control del ISBN-10 no es válido.");
        }

        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            int digit = isbn[i] - '0';
            sum += (10 - i) * digit;
        }

        int checkDigit;

        if (isbn[9] == 'X' || isbn[9] == 'x')
        {
            checkDigit = 10;
        }
        else
        {
            checkDigit = isbn[9] - '0';
        }

        sum += checkDigit;

        if (sum % 11 != 0)
        {
            throw new ArgumentException(
                "El ISBN-10 no es válido.");
        }
    }

    private static void ValidateISBN13(string isbn)
    {
        // ISBN-13 solamente admite dígitos.
        if (!isbn.All(char.IsDigit))
        {
            throw new ArgumentException(
                "El ISBN-13 debe contener únicamente 13 dígitos.");
        }

        // Un ISBN-13 válido debe comenzar normalmente
        // con 978 o 979.
        if (!isbn.StartsWith("978") &&
            !isbn.StartsWith("979"))
        {
            throw new ArgumentException(
                "El ISBN-13 debe comenzar con 978 o 979.");
        }

        int sum = 0;

        for (int i = 0; i < 12; i++)
        {
            int digit = isbn[i] - '0';

            sum += i % 2 == 0
                ? digit
                : digit * 3;
        }

        int calculatedCheckDigit =
            (10 - (sum % 10)) % 10;

        int providedCheckDigit =
            isbn[12] - '0';

        if (calculatedCheckDigit != providedCheckDigit)
        {
            throw new ArgumentException(
                "El ISBN-13 no es válido.");
        }
    }

    // ============================================================
    // AÑO DE PUBLICACIÓN
    // ============================================================

    private static void ValidatePublicationYear(int? publicationYear)
    {
        if (!publicationYear.HasValue)
        {
            return;
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

    private static void ValidatePublisher(string? publisher)
    {
        if (string.IsNullOrWhiteSpace(publisher))
        {
            return;
        }

        var normalizedPublisher = NormalizeSpaces(publisher);

        if (normalizedPublisher.Length > 20)
        {
            throw new ArgumentException(
                "La editorial no puede superar los 20 caracteres.");
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

    private static void ValidatePageCount(int? pageCount)
    {
        if (!pageCount.HasValue)
        {
            return;
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

    private static void ValidateDescription(string? description)
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
    // UTILIDADES
    // ============================================================

    private static string NormalizeSpaces(string value)
    {
        return Regex.Replace(
            value.Trim(),
            @"\s+",
            " ");
    }

    private static bool ContainsValidText(string value)
    {
        // Permite letras, números, espacios y puntuación
        // habitual en títulos/editoriales.
        return value.All(c =>
            char.IsLetterOrDigit(c) ||
            char.IsWhiteSpace(c) ||
            char.IsPunctuation(c));
    }
}
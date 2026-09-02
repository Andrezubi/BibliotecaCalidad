using Backend.Domain.Entities;

namespace Backend.Domain.Validators;

public static class BookValidator
{
    public static void Validate(Book book)
    {
        ValidateTitle(book.Title);
        ValidateEditionNumber(book.EditionNumber);
        ValidateISBN(book.ISBN);
        ValidatePublicationYear(book.PublicationYear);
        ValidatePublisher(book.Publisher);
        ValidatePageCount(book.PageCount);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "El título del libro es obligatorio.");
        }

        if (title.Length > 255)
        {
            throw new ArgumentException(
                "El título no puede superar los 255 caracteres.");
        }
    }

    private static void ValidateEditionNumber(int editionNumber)
    {
        if (editionNumber <= 0)
        {
            throw new ArgumentException(
                "El número de edición debe ser mayor a 0.");
        }
    }

    private static void ValidateISBN(string? isbn)
    {
        if (isbn is not null && isbn.Length > 20)
        {
            throw new ArgumentException(
                "El ISBN no puede superar los 20 caracteres.");
        }
    }

    private static void ValidatePublicationYear(int? publicationYear)
    {
        if (!publicationYear.HasValue)
        {
            return;
        }

        if (publicationYear < 1000 ||
            publicationYear > 2100)
        {
            throw new ArgumentException(
                "El año de publicación debe estar entre 1000 y 2100.");
        }
    }

    private static void ValidatePublisher(string? publisher)
    {
        if (publisher is not null && publisher.Length > 150)
        {
            throw new ArgumentException(
                "La editorial no puede superar los 150 caracteres.");
        }
    }

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
}
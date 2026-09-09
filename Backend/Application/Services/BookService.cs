using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Domain.Validators;
using Backend.Infraestructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly LibraryDbContext _context;

    private static readonly string[] AllowedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;

    public BookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        ICategoryRepository categoryRepository,
        IWebHostEnvironment environment,
        LibraryDbContext context)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _categoryRepository = categoryRepository;
        _environment = environment;
        _context = context;
    }

    // ======================================================
    // GET ALL
    // ======================================================

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        var books = await _bookRepository.GetAllAsync();

        var result = new List<BookDto>();

        foreach (var book in books)
        {
            result.Add(await MapToDtoAsync(book));
        }

        return result;
    }

    // ======================================================
    // GET BY ID
    // ======================================================

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
        {
            return null;
        }

        return await MapToDtoAsync(book);
    }

    // ======================================================
    // CREATE
    // ======================================================

    public async Task<BookDto> CreateAsync(CreateBookDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var authorIds = dto.AuthorIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        var categoryIds = dto.CategoryIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // ==================================================
        // VALIDAR AUTORES
        // ==================================================

        if (!authorIds.Any())
        {
            throw new InvalidOperationException(
                "Debe seleccionar al menos un autor.");
        }

        foreach (var authorId in authorIds)
        {
            var exists =
                await _authorRepository.ExistsActiveByIdAsync(authorId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"El autor con Id {authorId} no existe o está inactivo.");
            }
        }

        // ==================================================
        // VALIDAR CATEGORÍAS
        // ==================================================

        if (!categoryIds.Any())
        {
            throw new InvalidOperationException(
                "Debe seleccionar al menos una categoría.");
        }

        foreach (var categoryId in categoryIds)
        {
            var exists =
                await _categoryRepository.ExistsActiveByIdAsync(categoryId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"La categoría con Id {categoryId} no existe o está inactiva.");
            }
        }

        // ==================================================
        // VALIDAR PORTADA ANTES DE HACER CAMBIOS
        // ==================================================

        ValidateCoverFile(dto.CoverImage);

        // ==================================================
        // VALIDAR ISBN
        // ==================================================

        if (!string.IsNullOrWhiteSpace(dto.ISBN))
        {
            var isbnExists =
                await _bookRepository.ExistsByIsbnAsync(dto.ISBN);

            if (isbnExists)
            {
                throw new InvalidOperationException(
                    "Ya existe un libro activo con ese ISBN.");
            }
        }

        // ==================================================
        // CREAR LIBRO
        // ==================================================

        var book = new Book
        {
            Title = dto.Title,
            EditionNumber = dto.EditionNumber,
            ISBN = dto.ISBN,
            PublicationYear = dto.PublicationYear,
            Publisher = dto.Publisher,
            PageCount = dto.PageCount,
            Description = dto.Description,
            UserId = dto.UserId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        // ==================================================
        // CREAR RELACIONES EN MEMORIA
        // ==================================================

        foreach (var authorId in authorIds)
        {
            book.Bookauthors.Add(
                new Bookauthor
                {
                    Book = book,
                    AuthorId = authorId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UserId = dto.UserId
                });
        }

        foreach (var categoryId in categoryIds)
        {
            book.Bookcategories.Add(
                new Bookcategory
                {
                    Book = book,
                    CategoryId = categoryId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UserId = dto.UserId
                });
        }

        // ==================================================
        // VALIDAR LIBRO COMPLETO
        // ==================================================

        BookValidator.Validate(book);

        string? savedCoverPath = null;

        // ==================================================
        // TRANSACCIÓN
        // ==================================================

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // --------------------------------------------------
            // GUARDAR PORTADA
            // --------------------------------------------------

            if (dto.CoverImage != null &&
                dto.CoverImage.Length > 0)
            {
                savedCoverPath =
                    await SaveCoverImageAsync(dto.CoverImage);

                book.CoverImage = savedCoverPath;

                // Validamos nuevamente porque ahora Book tiene
                // la ruta real de la portada.
                BookValidator.Validate(book);
            }

            // --------------------------------------------------
            // GUARDAR LIBRO
            // --------------------------------------------------

            await _bookRepository.AddAsync(book);

            // --------------------------------------------------
            // GUARDAR RELACIONES
            // --------------------------------------------------

            foreach (var authorId in authorIds)
            {
                var bookAuthor =
                    new Bookauthor
                    {
                        BookId = book.Id,
                        AuthorId = authorId,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UserId = dto.UserId
                    };

                await _authorRepository.AddBookAuthorAsync(bookAuthor);
            }

            foreach (var categoryId in categoryIds)
            {
                var bookCategory =
                    new Bookcategory
                    {
                        BookId = book.Id,
                        CategoryId = categoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UserId = dto.UserId
                    };

                await _categoryRepository.AddBookCategoryAsync(bookCategory);
            }

            // --------------------------------------------------
            // CONFIRMAR TRANSACCIÓN
            // --------------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            // Si se guardó una portada pero falló la BD,
            // eliminamos el archivo para no dejarlo huérfano.
            if (!string.IsNullOrWhiteSpace(savedCoverPath))
            {
                DeleteCoverImage(savedCoverPath);
            }

            throw;
        }

        return await MapToDtoAsync(book);
    }

    // ======================================================
    // UPDATE
    // ======================================================

    public async Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
        {
            return null;
        }

        var authorIds = dto.AuthorIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        var categoryIds = dto.CategoryIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // ==================================================
        // VALIDAR AUTORES
        // ==================================================

        if (!authorIds.Any())
        {
            throw new InvalidOperationException(
                "Debe seleccionar al menos un autor.");
        }

        foreach (var authorId in authorIds)
        {
            var exists =
                await _authorRepository.ExistsActiveByIdAsync(authorId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"El autor con Id {authorId} no existe o está inactivo.");
            }
        }

        // ==================================================
        // VALIDAR CATEGORÍAS
        // ==================================================

        if (!categoryIds.Any())
        {
            throw new InvalidOperationException(
                "Debe seleccionar al menos una categoría.");
        }

        foreach (var categoryId in categoryIds)
        {
            var exists =
                await _categoryRepository.ExistsActiveByIdAsync(categoryId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"La categoría con Id {categoryId} no existe o está inactiva.");
            }
        }

        // ==================================================
        // VALIDAR PORTADA
        // ==================================================

        ValidateCoverFile(dto.CoverImage);

        // ==================================================
        // VALIDAR ISBN
        // ==================================================

        if (!string.IsNullOrWhiteSpace(dto.ISBN) &&
            dto.ISBN != book.ISBN)
        {
            var isbnExists =
                await _bookRepository.ExistsByIsbnAsync(dto.ISBN);

            if (isbnExists)
            {
                throw new InvalidOperationException(
                    "Ya existe un libro activo con ese ISBN.");
            }
        }

        // Guardamos la portada anterior.
        var oldCover = book.CoverImage;

        // ==================================================
        // ACTUALIZAR DATOS
        // ==================================================

        book.Title = dto.Title;
        book.EditionNumber = dto.EditionNumber;
        book.ISBN = dto.ISBN;
        book.PublicationYear = dto.PublicationYear;
        book.Publisher = dto.Publisher;
        book.PageCount = dto.PageCount;
        book.Description = dto.Description;
        book.UserId = dto.UserId;
        book.UpdatedAt = DateTime.Now;

        // ==================================================
        // PREPARAR RELACIONES PARA VALIDACIÓN
        // ==================================================

        book.Bookauthors.Clear();

        foreach (var authorId in authorIds)
        {
            book.Bookauthors.Add(
                new Bookauthor
                {
                    BookId = book.Id,
                    AuthorId = authorId,
                    IsActive = true,
                    UserId = dto.UserId
                });
        }

        book.Bookcategories.Clear();

        foreach (var categoryId in categoryIds)
        {
            book.Bookcategories.Add(
                new Bookcategory
                {
                    BookId = book.Id,
                    CategoryId = categoryId,
                    IsActive = true,
                    UserId = dto.UserId
                });
        }

        // ==================================================
        // VALIDAR LIBRO
        // ==================================================

        BookValidator.Validate(book);

        string? newCoverPath = null;

        // ==================================================
        // TRANSACCIÓN
        // ==================================================

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // --------------------------------------------------
            // GUARDAR NUEVA PORTADA
            // --------------------------------------------------

            if (dto.CoverImage != null &&
                dto.CoverImage.Length > 0)
            {
                newCoverPath =
                    await SaveCoverImageAsync(dto.CoverImage);

                book.CoverImage = newCoverPath;

                BookValidator.Validate(book);
            }

            // --------------------------------------------------
            // ACTUALIZAR LIBRO
            // --------------------------------------------------

            await _bookRepository.UpdateAsync(book);

            // --------------------------------------------------
            // ACTUALIZAR AUTORES
            // --------------------------------------------------

            await _bookRepository.UpdateAuthorsAsync(
                id,
                authorIds,
                dto.UserId);

            // --------------------------------------------------
            // ACTUALIZAR CATEGORÍAS
            // --------------------------------------------------

            await _bookRepository.UpdateCategoriesAsync(
                id,
                categoryIds,
                dto.UserId);

            // --------------------------------------------------
            // CONFIRMAR
            // --------------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            // La nueva portada se elimina porque la actualización
            // completa no llegó a terminar.
            if (!string.IsNullOrWhiteSpace(newCoverPath))
            {
                DeleteCoverImage(newCoverPath);
            }

            throw;
        }

        // ==================================================
        // ELIMINAR PORTADA ANTERIOR
        // ==================================================

        if (!string.IsNullOrWhiteSpace(newCoverPath) &&
            !string.IsNullOrWhiteSpace(oldCover))
        {
            DeleteCoverImage(oldCover);
        }

        return await MapToDtoAsync(book);
    }

    // ======================================================
    // DELETE
    // ======================================================

    public async Task<bool> DeleteAsync(int id)
    {
        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
        {
            return false;
        }

        await _bookRepository.DeleteAsync(book);

        return true;
    }

    // ======================================================
    // SEARCH
    // ======================================================

    public async Task<IEnumerable<BookDto>> SearchAsync(
        string? phrase = null)
    {
        var books =
            await _bookRepository.SearchAsync(phrase);

        var result = new List<BookDto>();

        foreach (var book in books)
        {
            result.Add(await MapToDtoAsync(book));
        }

        return result;
    }

    // ======================================================
    // VALIDAR ARCHIVO DE PORTADA
    // ======================================================

    private static void ValidateCoverFile(
        IFormFile? file)
    {
        if (file == null)
        {
            return;
        }

        if (file.Length == 0)
        {
            throw new ArgumentException(
                "La imagen de portada está vacía.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "La imagen de portada no puede superar los 5 MB.");
        }

        var extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "El formato de la portada debe ser JPG, JPEG, PNG o WEBP.");
        }
    }

    // ======================================================
    // SAVE COVER IMAGE
    // ======================================================

    private async Task<string> SaveCoverImageAsync(
        IFormFile file)
    {
        ValidateCoverFile(file);

        var webRootPath =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath =
                Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
        }

        var uploadsPath =
            Path.Combine(
                webRootPath,
                "uploads",
                "books");

        Directory.CreateDirectory(uploadsPath);

        var extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        var fileName =
            $"{Guid.NewGuid():N}{extension}";

        var filePath =
            Path.Combine(
                uploadsPath,
                fileName);

        await using var stream =
            new FileStream(
                filePath,
                FileMode.CreateNew);

        await file.CopyToAsync(stream);

        return $"/uploads/books/{fileName}";
    }

    // ======================================================
    // DELETE COVER IMAGE
    // ======================================================

    private void DeleteCoverImage(
        string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(coverImage))
        {
            return;
        }

        var fileName =
            Path.GetFileName(coverImage);

        var webRootPath =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath =
                Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
        }

        var filePath =
            Path.Combine(
                webRootPath,
                "uploads",
                "books",
                fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // ======================================================
    // MAP ENTITY -> DTO
    // ======================================================

    private async Task<BookDto> MapToDtoAsync(
        Book book)
    {
        var authorIds =
            await _bookRepository
                .GetAuthorIdsAsync(book.Id);

        var categoryIds =
            await _bookRepository
                .GetCategoryIdsAsync(book.Id);

        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            EditionNumber = book.EditionNumber,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            Publisher = book.Publisher,
            PageCount = book.PageCount,
            Description = book.Description,
            CoverImage = book.CoverImage,
            IsActive = book.IsActive,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt,
            UserId = book.UserId,
            AuthorIds = authorIds.ToList(),
            CategoryIds = categoryIds.ToList()
        };
    }
}
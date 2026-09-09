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

    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const long MaxFileSize =
        5 * 1024 * 1024;

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

    // ============================================================
    // OBTENER TODOS
    // ============================================================

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        var books =
            await _bookRepository.GetAllAsync();

        var result =
            new List<BookDto>();

        foreach (var book in books)
        {
            result.Add(
                await MapToDtoAsync(book));
        }

        return result;
    }

    // ============================================================
    // OBTENER POR ID
    // ============================================================

    public async Task<BookDto?> GetByIdAsync(
        int id)
    {
        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null ||
            !book.IsActive)
        {
            return null;
        }

        return await MapToDtoAsync(book);
    }

    // ============================================================
    // CREAR LIBRO
    // ============================================================

    public async Task<BookDto> CreateAsync(
        CreateBookDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(
                nameof(dto));
        }

        // --------------------------------------------------------
        // NORMALIZAR AUTORES
        // --------------------------------------------------------

        var authorIds = dto.AuthorIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // --------------------------------------------------------
        // NORMALIZAR CATEGORÍAS
        // --------------------------------------------------------

        var categoryIds = dto.CategoryIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // --------------------------------------------------------
        // VALIDAR AUTORES
        // --------------------------------------------------------

        BookValidator.ValidateAuthorIds(
            authorIds);

        foreach (var authorId in authorIds)
        {
            var exists =
                await _authorRepository
                    .ExistsActiveByIdAsync(authorId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"El autor con Id {authorId} no existe o está inactivo.");
            }
        }

        // --------------------------------------------------------
        // VALIDAR CATEGORÍAS
        // --------------------------------------------------------

        BookValidator.ValidateCategoryIds(
            categoryIds);

        foreach (var categoryId in categoryIds)
        {
            var exists =
                await _categoryRepository
                    .ExistsActiveByIdAsync(categoryId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"La categoría con Id {categoryId} no existe o está inactiva.");
            }
        }

        // --------------------------------------------------------
        // VALIDAR PORTADA
        // --------------------------------------------------------

        ValidateCoverFile(
            dto.CoverImage);

        // --------------------------------------------------------
        // VALIDAR ISBN
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(dto.ISBN))
        {
            var isbnExists =
                await _bookRepository
                    .ExistsByIsbnAsync(dto.ISBN);

            if (isbnExists)
            {
                throw new InvalidOperationException(
                    "Ya existe un libro activo con ese ISBN.");
            }
        }

        // --------------------------------------------------------
        // CREAR ENTIDAD
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // VALIDAR DATOS DEL LIBRO
        // --------------------------------------------------------

        BookValidator.Validate(book);

        string? savedCoverPath = null;

        // --------------------------------------------------------
        // TRANSACCIÓN
        // --------------------------------------------------------

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            // ----------------------------------------------------
            // GUARDAR PORTADA
            // ----------------------------------------------------

            if (dto.CoverImage != null &&
                dto.CoverImage.Length > 0)
            {
                savedCoverPath =
                    await SaveCoverImageAsync(
                        dto.CoverImage);

                book.CoverImage =
                    savedCoverPath;

                BookValidator.Validate(
                    book);
            }

            // ----------------------------------------------------
            // GUARDAR LIBRO
            // ----------------------------------------------------

            await _bookRepository.AddAsync(
                book);

            // ----------------------------------------------------
            // GUARDAR AUTORES
            // ----------------------------------------------------

            foreach (var authorId in authorIds)
            {
                await _authorRepository
                    .AddBookAuthorAsync(
                        new Bookauthor
                        {
                            BookId = book.Id,
                            AuthorId = authorId,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UserId = dto.UserId
                        });
            }

            // ----------------------------------------------------
            // GUARDAR CATEGORÍAS
            // ----------------------------------------------------

            foreach (var categoryId in categoryIds)
            {
                await _categoryRepository
                    .AddBookCategoryAsync(
                        new Bookcategory
                        {
                            BookId = book.Id,
                            CategoryId = categoryId,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UserId = dto.UserId
                        });
            }

            // ----------------------------------------------------
            // CONFIRMAR
            // ----------------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            // ----------------------------------------------------
            // DESHACER BD
            // ----------------------------------------------------

            await transaction.RollbackAsync();

            // ----------------------------------------------------
            // ELIMINAR PORTADA
            // ----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    savedCoverPath))
            {
                DeleteCoverImage(
                    savedCoverPath);
            }

            throw;
        }

        return await MapToDtoAsync(
            book);
    }

    // ============================================================
    // ACTUALIZAR LIBRO
    // ============================================================

    public async Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(
                nameof(dto));
        }

        // --------------------------------------------------------
        // OBTENER LIBRO
        // --------------------------------------------------------

        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book == null ||
            !book.IsActive)
        {
            return null;
        }

        // --------------------------------------------------------
        // NORMALIZAR AUTORES
        // --------------------------------------------------------

        var authorIds = dto.AuthorIds?
            .Where(authorId => authorId > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // --------------------------------------------------------
        // NORMALIZAR CATEGORÍAS
        // --------------------------------------------------------

        var categoryIds = dto.CategoryIds?
            .Where(categoryId => categoryId > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();

        // --------------------------------------------------------
        // VALIDAR AUTORES
        // --------------------------------------------------------

        BookValidator.ValidateAuthorIds(
            authorIds);

        foreach (var authorId in authorIds)
        {
            var exists =
                await _authorRepository
                    .ExistsActiveByIdAsync(
                        authorId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"El autor con Id {authorId} no existe o está inactivo.");
            }
        }

        // --------------------------------------------------------
        // VALIDAR CATEGORÍAS
        // --------------------------------------------------------

        BookValidator.ValidateCategoryIds(
            categoryIds);

        foreach (var categoryId in categoryIds)
        {
            var exists =
                await _categoryRepository
                    .ExistsActiveByIdAsync(
                        categoryId);

            if (!exists)
            {
                throw new InvalidOperationException(
                    $"La categoría con Id {categoryId} no existe o está inactiva.");
            }
        }

        // --------------------------------------------------------
        // VALIDAR PORTADA
        // --------------------------------------------------------

        ValidateCoverFile(
            dto.CoverImage);

        // --------------------------------------------------------
        // VALIDAR ISBN
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(dto.ISBN) &&
            !string.Equals(
                dto.ISBN,
                book.ISBN,
                StringComparison.Ordinal))
        {
            var isbnExists =
                await _bookRepository
                    .ExistsByIsbnAsync(
                        dto.ISBN);

            if (isbnExists)
            {
                throw new InvalidOperationException(
                    "Ya existe un libro activo con ese ISBN.");
            }
        }

        // --------------------------------------------------------
        // GUARDAR PORTADA ANTERIOR
        // --------------------------------------------------------

        var oldCover =
            book.CoverImage;

        // --------------------------------------------------------
        // ACTUALIZAR DATOS
        // --------------------------------------------------------

        book.Title =
            dto.Title;

        book.EditionNumber =
            dto.EditionNumber;

        book.ISBN =
            dto.ISBN;

        book.PublicationYear =
            dto.PublicationYear;

        book.Publisher =
            dto.Publisher;

        book.PageCount =
            dto.PageCount;

        book.Description =
            dto.Description;

        book.UserId =
            dto.UserId;

        book.UpdatedAt =
            DateTime.Now;

        // IMPORTANTE:
        // NO hacemos Clear() de Bookauthors
        // NO hacemos Clear() de Bookcategories

        BookValidator.Validate(
            book);

        string? newCoverPath = null;

        // --------------------------------------------------------
        // TRANSACCIÓN
        // --------------------------------------------------------

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            // ----------------------------------------------------
            // GUARDAR NUEVA PORTADA
            // ----------------------------------------------------

            if (dto.CoverImage != null &&
                dto.CoverImage.Length > 0)
            {
                newCoverPath =
                    await SaveCoverImageAsync(
                        dto.CoverImage);

                book.CoverImage =
                    newCoverPath;

                BookValidator.Validate(
                    book);
            }

            // ----------------------------------------------------
            // ACTUALIZAR LIBRO
            // ----------------------------------------------------

            await _bookRepository
                .UpdateAsync(book);

            // ----------------------------------------------------
            // ACTUALIZAR AUTORES
            // ----------------------------------------------------

            await _bookRepository
                .UpdateAuthorsAsync(
                    id,
                    authorIds,
                    dto.UserId);

            // ----------------------------------------------------
            // ACTUALIZAR CATEGORÍAS
            // ----------------------------------------------------

            await _bookRepository
                .UpdateCategoriesAsync(
                    id,
                    categoryIds,
                    dto.UserId);

            // ----------------------------------------------------
            // CONFIRMAR TRANSACCIÓN
            // ----------------------------------------------------

            await transaction.CommitAsync();
        }
        catch
        {
            // ----------------------------------------------------
            // DESHACER BD
            // ----------------------------------------------------

            await transaction.RollbackAsync();

            // ----------------------------------------------------
            // ELIMINAR NUEVA PORTADA
            // ----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    newCoverPath))
            {
                DeleteCoverImage(
                    newCoverPath);
            }

            throw;
        }

        // --------------------------------------------------------
        // ELIMINAR PORTADA ANTERIOR
        // SOLO DESPUÉS DEL COMMIT
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                newCoverPath) &&
            !string.IsNullOrWhiteSpace(
                oldCover))
        {
            DeleteCoverImage(
                oldCover);
        }

        return await MapToDtoAsync(
            book);
    }

    // ============================================================
    // ELIMINACIÓN LÓGICA
    // ============================================================

    public async Task<bool> DeleteAsync(
        int id)
    {
        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book == null ||
            !book.IsActive)
        {
            return false;
        }

        await _bookRepository
            .DeleteAsync(book);

        return true;
    }

    // ============================================================
    // BÚSQUEDA
    // ============================================================

    public async Task<IEnumerable<BookDto>> SearchAsync(
        string? phrase = null)
    {
        var books =
            await _bookRepository
                .SearchAsync(phrase);

        var result =
            new List<BookDto>();

        foreach (var book in books)
        {
            result.Add(
                await MapToDtoAsync(book));
        }

        return result;
    }

    // ============================================================
    // VALIDAR PORTADA
    // ============================================================

    private static void ValidateCoverFile(
        IFormFile? file)
    {
        // La portada es opcional.
        if (file == null)
        {
            return;
        }

        if (file.Length <= 0)
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
            Path.GetExtension(
                file.FileName)
                .ToLowerInvariant();

        if (!AllowedExtensions.Contains(
                extension))
        {
            throw new ArgumentException(
                "El formato de la portada debe ser JPG, JPEG, PNG o WEBP.");
        }

        if (!string.IsNullOrWhiteSpace(
                file.ContentType) &&
            !AllowedContentTypes.Contains(
                file.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException(
                "El tipo de archivo de la portada no es válido.");
        }
    }

    // ============================================================
    // GUARDAR PORTADA
    // ============================================================

    private async Task<string> SaveCoverImageAsync(
        IFormFile file)
    {
        ValidateCoverFile(
            file);

        var webRootPath =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(
                webRootPath))
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

        Directory.CreateDirectory(
            uploadsPath);

        var extension =
            Path.GetExtension(
                file.FileName)
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

        await file.CopyToAsync(
            stream);

        return $"/uploads/books/{fileName}";
    }

    // ============================================================
    // ELIMINAR PORTADA
    // ============================================================

    private void DeleteCoverImage(
        string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(
                coverImage))
        {
            return;
        }

        var fileName =
            Path.GetFileName(
                coverImage);

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            return;
        }

        var webRootPath =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(
                webRootPath))
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

        if (File.Exists(
                filePath))
        {
            File.Delete(
                filePath);
        }
    }

    // ============================================================
    // MAPEAR A DTO
    // ============================================================

    private async Task<BookDto> MapToDtoAsync(
        Book book)
    {
        var authorIds =
            await _bookRepository
                .GetAuthorIdsAsync(
                    book.Id);

        var categoryIds =
            await _bookRepository
                .GetCategoryIdsAsync(
                    book.Id);

        return new BookDto
        {
            Id =
                book.Id,

            Title =
                book.Title,

            EditionNumber =
                book.EditionNumber,

            ISBN =
                book.ISBN,

            PublicationYear =
                book.PublicationYear,

            Publisher =
                book.Publisher,

            PageCount =
                book.PageCount,

            Description =
                book.Description,

            CoverImage =
                book.CoverImage,

            IsActive =
                book.IsActive,

            CreatedAt =
                book.CreatedAt,

            UpdatedAt =
                book.UpdatedAt,

            UserId =
                book.UserId,

            AuthorIds =
                authorIds.ToList(),

            CategoryIds =
                categoryIds.ToList()
        };
    }
}
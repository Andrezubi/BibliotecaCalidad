using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Validators;
using Backend.Models;
using Backend.Infraestructure.Persistence;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly LibraryDbcontext _context;

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
        LibraryDbcontext context)
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

        return await MapWithAvailabilityAsync(books);
    }

    public async Task<IEnumerable<BookDto>> GetAvailableAsync()
    {
        var books =
            await _bookRepository.GetAvailableAsync();

        return await MapWithAvailabilityAsync(books);
    }

    // ============================================================
    // OBTENER POR ID
    // ============================================================

    public async Task<BookDto?> GetByIdAsync(uint id)
    {
        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null || book.IsActive != true)
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
        ArgumentNullException.ThrowIfNull(dto);

        var authorIds =
            NormalizeAuthorIds(dto.AuthorIds);

        var categoryIds =
            NormalizeCategoryIds(dto.CategoryIds);

        await ValidateAuthorsAsync(authorIds);
        await ValidateCategoriesAsync(categoryIds);

        ValidateCoverFile(dto.CoverImage);

        await ValidateIsbnAsync(dto.ISBN);

        var book =
            CreateBookEntity(dto);

        BookValidator.Validate(book);

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        string? savedCoverPath = null;

        try
        {
            savedCoverPath =
                await SaveCoverAsync(
                    dto.CoverImage);

            if (!string.IsNullOrWhiteSpace(
                    savedCoverPath))
            {
                book.CoverImage =
                    savedCoverPath;

                BookValidator.Validate(book);
            }

            await _bookRepository.AddAsync(book);

            await AddAuthorsAsync(
                book.Id,
                authorIds,
                dto.UserId);

            await AddCategoriesAsync(
                book.Id,
                categoryIds,
                dto.UserId);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            DeleteCoverImage(
                savedCoverPath);

            throw;
        }

        return await MapToDtoAsync(book);
    }

    // ============================================================
    // ACTUALIZAR LIBRO
    // ============================================================

    public async Task<BookDto?> UpdateAsync(
        uint id,
        UpdateBookDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null || book.IsActive != true)
            return null;

        var authorIds = NormalizeAuthorIds(dto.AuthorIds);
        var categoryIds = NormalizeCategoryIds(dto.CategoryIds);

        await ValidateAuthorsAsync(authorIds);
        await ValidateCategoriesAsync(categoryIds);

        ValidateCoverFile(dto.CoverImage);

        await ValidateUpdateIsbnAsync(
            dto.ISBN,
            book.Isbn);

        var oldCover = book.CoverImage;

        UpdateBookEntity(book, dto);

        BookValidator.Validate(book);

        var newCoverPath = await UpdateBookTransactionAsync(
            book,
            dto,
            authorIds,
            categoryIds);

        DeleteCoverImage(
            oldCover,
            newCoverPath);

        return await MapToDtoAsync(book);
    }


    private async Task<string?> UpdateBookTransactionAsync(
    Book book,
    UpdateBookDto dto,
    IEnumerable<uint> authorIds,
    IEnumerable<byte> categoryIds)
    {
        string? newCoverPath = null;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            newCoverPath = await SaveCoverAsync(
                dto.CoverImage);

            if (!string.IsNullOrWhiteSpace(newCoverPath))
            {
                book.CoverImage = newCoverPath;

                BookValidator.Validate(book);
            }

            await _bookRepository.UpdateAsync(book);

            await _bookRepository.UpdateAuthorsAsync(
                book.Id,
                authorIds.ToList(),
                dto.UserId != null ? (ushort)dto.UserId.Value : null);

            await _bookRepository.UpdateCategoriesAsync(
                book.Id,
                categoryIds.ToList(),
                dto.UserId != null ? (ushort)dto.UserId.Value : null);

            await transaction.CommitAsync();

            return newCoverPath;
        }
        catch
        {
            await transaction.RollbackAsync();

            DeleteCoverImage(newCoverPath);

            throw;
        }
    }


    // ============================================================
    // ELIMINACIÓN LÓGICA
    // ============================================================

    public async Task<bool> DeleteAsync(uint id)
    {
        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book == null ||
            book.IsActive != true)
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
    // AGREGAR COPIA
    // ============================================================

    public async Task<string?> AddCopyAsync(
        uint bookId,
        string internalCode)
    {
        if (string.IsNullOrWhiteSpace(internalCode))
        {
            return "El código interno es obligatorio.";
        }

        internalCode =
            internalCode.Trim();

        if (internalCode.Length > 50)
        {
            return "El código interno no puede superar los 50 caracteres.";
        }

        var book =
            await _bookRepository
                .GetByIdAsync(bookId);

        if (book == null ||
            book.IsActive != true)
        {
            return "El libro no existe.";
        }

        if (await _bookRepository
                .InternalCodeExistsAsync(
                    internalCode))
        {
            return "Ya existe una copia con ese código interno.";
        }

        var copy = new Copy
        {
            BookId = bookId,
            InternalCode = internalCode,
            StatusId = 1, // Available
            CreatedAt = DateTime.Now
        };

        await _bookRepository
            .AddCopyAsync(copy);

        return null;
    }

    // ============================================================
    // NORMALIZAR IDS
    // ============================================================

    private static List<uint> NormalizeAuthorIds(
        IEnumerable<int>? ids)
    {
        return ids?
            .Where(id => id > 0)
            .Select(id => (uint)id)
            .Distinct()
            .ToList()
            ?? new List<uint>();
    }

    private static List<byte> NormalizeCategoryIds(
        IEnumerable<int>? ids)
    {
        return ids?
            .Where(id => id > 0)
            .Select(id => (byte)id)
            .Distinct()
            .ToList()
            ?? new List<byte>();
    }

    // ============================================================
    // VALIDAR AUTORES
    // ============================================================

    private async Task ValidateAuthorsAsync(
        IEnumerable<uint> authorIds)
    {
        var ids =
            authorIds.ToList();

        BookValidator.ValidateAuthorIds(ids);

        foreach (var authorId in ids)
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
    }

    // ============================================================
    // VALIDAR CATEGORÍAS
    // ============================================================

    private async Task ValidateCategoriesAsync(
        IEnumerable<byte> categoryIds)
    {
        var ids =
            categoryIds.ToList();

        BookValidator.ValidateCategoryIds(ids);

        foreach (var categoryId in ids)
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
    }

    // ============================================================
    // VALIDAR ISBN AL CREAR
    // ============================================================

    private async Task ValidateIsbnAsync(
        string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return;
        }

        var exists =
            await _bookRepository
                .ExistsByIsbnAsync(isbn);

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe un libro activo con ese ISBN.");
        }
    }

    // ============================================================
    // VALIDAR ISBN AL ACTUALIZAR
    // ============================================================

    private async Task ValidateUpdateIsbnAsync(
        string? newIsbn,
        string? currentIsbn)
    {
        if (string.IsNullOrWhiteSpace(newIsbn) ||
            string.Equals(
                newIsbn,
                currentIsbn,
                StringComparison.Ordinal))
        {
            return;
        }

        var exists =
            await _bookRepository
                .ExistsByIsbnAsync(newIsbn);

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe un libro activo con ese ISBN.");
        }
    }

    // ============================================================
    // CREAR ENTIDAD BOOK
    // ============================================================

    private static Book CreateBookEntity(
        CreateBookDto dto)
    {
        return new Book
        {
            Title = dto.Title,
            EditionNumber = (byte)(dto.EditionNumber ?? 1),
            Isbn = dto.ISBN,
            PublicationYear = dto.PublicationYear != null ? (ushort)dto.PublicationYear : null,
            PublisherId = dto.PublisherId,
            PageCount = dto.PageCount != null ? (ushort)dto.PageCount : null,
            Description = dto.Description,
            UserId = dto.UserId != null ? (ushort)dto.UserId : null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }

    // ============================================================
    // ACTUALIZAR ENTIDAD BOOK
    // ============================================================

    private static void UpdateBookEntity(
        Book book,
        UpdateBookDto dto)
    {
        book.Title =
            dto.Title;

        book.EditionNumber =
            dto.EditionNumber.HasValue ? (byte)dto.EditionNumber.Value : book.EditionNumber;

        book.Isbn =
            dto.ISBN;

        book.PublicationYear =
            dto.PublicationYear.HasValue ? (ushort)dto.PublicationYear.Value : book.PublicationYear;

        book.PublisherId =
            dto.PublisherId;

        book.PageCount =
            dto.PageCount.HasValue ? (ushort)dto.PageCount.Value : book.PageCount;

        book.Description =
            dto.Description;

        book.UserId =
            dto.UserId.HasValue ? (ushort)dto.UserId.Value : book.UserId;

        book.UpdatedAt =
            DateTime.Now;
    }

    // ============================================================
    // AGREGAR AUTORES
    // ============================================================

    private async Task AddAuthorsAsync(
        uint bookId,
        IEnumerable<uint> authorIds,
        int? userId)
    {
        foreach (var authorId in authorIds)
        {
            var bookAuthor =
                new Bookauthor
                {
                    BookId = bookId,
                    AuthorId = authorId,
                    CreatedAt = DateTime.Now,
                    UserId = userId != null ? (ushort)userId.Value : null
                };

            await _authorRepository
                .AddBookAuthorAsync(
                    bookAuthor);
        }
    }

    // ============================================================
    // AGREGAR CATEGORÍAS
    // ============================================================

    private async Task AddCategoriesAsync(
        uint bookId,
        IEnumerable<byte> categoryIds,
        int? userId)
    {
        foreach (var categoryId in categoryIds)
        {
            var bookCategory =
                new Bookcategory
                {
                    BookId = bookId,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.Now,
                    UserId = userId != null ? (ushort)userId.Value : null
                };

            await _categoryRepository
                .AddBookCategoryAsync(
                    bookCategory);
        }
    }

    // ============================================================
    // VALIDAR PORTADA
    // ============================================================

    private static void ValidateCoverFile(
        IFormFile? file)
    {
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
                file.ContentType
                    .ToLowerInvariant()))
        {
            throw new ArgumentException(
                "El tipo de archivo de la portada no es válido.");
        }
    }

    // ============================================================
    // GUARDAR PORTADA
    // ============================================================

    private async Task<string?> SaveCoverAsync(
        IFormFile? file)
    {
        if (file == null ||
            file.Length <= 0)
        {
            return null;
        }

        var webRootPath =
            GetWebRootPath();

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
    // OBTENER WWWROOT
    // ============================================================

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(
                _environment.WebRootPath))
        {
            return _environment.WebRootPath;
        }

        return Path.Combine(
            _environment.ContentRootPath,
            "wwwroot");
    }

    // ============================================================
    // ELIMINAR PORTADA
    // ============================================================

    private void DeleteCoverImage(
        string? coverImage,
        string? exceptPath = null)
    {
        if (string.IsNullOrWhiteSpace(
                coverImage))
        {
            return;
        }

        if (string.Equals(
                coverImage,
                exceptPath,
                StringComparison.OrdinalIgnoreCase))
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

        var filePath =
            Path.Combine(
                GetWebRootPath(),
                "uploads",
                "books",
                fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
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
                (int)book.Id,

            Title =
                book.Title,

            EditionNumber =
                book.EditionNumber,

            ISBN =
                book.Isbn,

            PublicationYear =
                book.PublicationYear != null ? (int)book.PublicationYear.Value : null,

            PublisherId =
                book.PublisherId,

            Publisher =
                book.Publisher?.Name,

            PageCount =
                book.PageCount != null ? (int)book.PageCount.Value : null,

            Description =
                book.Description,

            CoverImage =
                book.CoverImage,

            IsActive =
                book.IsActive ?? true,

            CreatedAt =
                book.CreatedAt,

            UpdatedAt =
                book.UpdatedAt,

            UserId =
                book.UserId != null ? (int)book.UserId.Value : null,

            AuthorIds =
                authorIds.Select(x => (int)x).ToList(),

            CategoryIds =
                categoryIds.Select(x => (int)x).ToList()
        };
    }

    // ============================================================
    // MAPEAR CON DISPONIBILIDAD
    // ============================================================

    private async Task<List<BookDto>>
        MapWithAvailabilityAsync(
            IEnumerable<Book> books)
    {
        var result =
            new List<BookDto>();

        foreach (var book in books)
        {
            var dto =
                await MapToDtoAsync(book);

            dto.AvailableCopies =
                await _bookRepository
                    .CountAvailableCopiesAsync(
                        book.Id);

            result.Add(dto);
        }

        return result;
    }
}
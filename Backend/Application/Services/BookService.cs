using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Models;
using Backend.Domain.Interfaces;
using Backend.Domain.Validators;
using Microsoft.AspNetCore.Http;

namespace Backend.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IWebHostEnvironment _environment;

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
        IWebHostEnvironment environment)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _categoryRepository = categoryRepository;
        _environment = environment;
    }

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        var books = await _bookRepository.GetAllAsync();

        return books.Select(MapToDto);
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
            return null;

        return MapToDto(book);
    }

    // ======================================================
    // CREATE
    // ======================================================

    public async Task<BookDto> CreateAsync(CreateBookDto dto)
    {
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

        BookValidator.Validate(book);

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

        // Guardar portada
        if (dto.CoverImage != null)
        {
            book.CoverImage =
                await SaveCoverImageAsync(dto.CoverImage);
        }

        await _bookRepository.AddAsync(book);

        // ==========================================
        // EXISTING AUTHORS
        // ==========================================

        foreach (var authorId in dto.AuthorIds.Distinct())
        {
            var bookAuthor = new Bookauthor
            {
                BookId = book.Id,
                AuthorId = authorId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UserId = dto.UserId
            };

            await _authorRepository.AddBookAuthorAsync(bookAuthor);
        }

        // ==========================================
        // NEW AUTHORS
        // ==========================================

        foreach (var newAuthor in dto.NewAuthors)
        {
            var author = new Author
            {
                FirstName = newAuthor.FirstName,
                LastName = newAuthor.LastName,
                UserId = dto.UserId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _authorRepository.AddAsync(author);

            var bookAuthor = new Bookauthor
            {
                BookId = book.Id,
                AuthorId = author.Id,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UserId = dto.UserId
            };

            await _authorRepository.AddBookAuthorAsync(bookAuthor);
        }

        // ==========================================
        // EXISTING CATEGORIES
        // ==========================================

        foreach (var categoryId in dto.CategoryIds.Distinct())
        {
            var bookCategory = new Bookcategory
            {
                BookId = book.Id,
                CategoryId = categoryId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UserId = dto.UserId
            };

            await _categoryRepository.AddBookCategoryAsync(bookCategory);
        }

        // ==========================================
        // NEW CATEGORIES
        // ==========================================

        foreach (var newCategory in dto.NewCategories)
        {
            var category = new Category
            {
                Name = newCategory.Name,
                Description = newCategory.Description,
                UserId = dto.UserId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _categoryRepository.AddAsync(category);

            var bookCategory = new Bookcategory
            {
                BookId = book.Id,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UserId = dto.UserId
            };

            await _categoryRepository.AddBookCategoryAsync(bookCategory);
        }




        return MapToDto(book);
    }

    // ======================================================
    // UPDATE
    // ======================================================

    public async Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto dto)
    {
        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
            return null;

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

        book.Title = dto.Title;
        book.EditionNumber = dto.EditionNumber;
        book.ISBN = dto.ISBN;
        book.PublicationYear = dto.PublicationYear;
        book.Publisher = dto.Publisher;
        book.PageCount = dto.PageCount;
        book.Description = dto.Description;
        book.UserId = null;
        book.UpdatedAt = DateTime.Now;

        BookValidator.Validate(book);

        // Si se seleccionó una nueva portada
        if (dto.CoverImage != null)
        {
            var oldCover = book.CoverImage;

            book.CoverImage =
                await SaveCoverImageAsync(dto.CoverImage);

            // Eliminar portada anterior
            DeleteCoverImage(oldCover);
        }

        await _bookRepository.UpdateAsync(book);

        return MapToDto(book);
    }

    // ======================================================
    // DELETE
    // ======================================================

    public async Task<bool> DeleteAsync(int id)
    {
        var book =
            await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
            return false;

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

        return books.Select(MapToDto);
    }

    // ======================================================
    // SAVE IMAGE
    // ======================================================

    private async Task<string> SaveCoverImageAsync(
     IFormFile file)
    {
        if (file == null)
        {
            throw new ArgumentException(
                "No se recibió la imagen de portada.");
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

        // Obtener la carpeta wwwroot de forma segura
        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");
        }

        // Crear:
        // Backend/wwwroot/uploads/books
        var uploadsPath =
            Path.Combine(
                webRootPath,
                "uploads",
                "books");

        Directory.CreateDirectory(uploadsPath);

        // Generar un nombre único para evitar conflictos
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

        // Esto es lo único que se guarda en MySQL
        return $"/uploads/books/{fileName}";
    }
    // ======================================================
    // DELETE OLD IMAGE
    // ======================================================

    private void DeleteCoverImage(string? coverImage)
    {
        if (string.IsNullOrWhiteSpace(coverImage))
            return;

        var fileName =
            Path.GetFileName(coverImage);

        var filePath =
            Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "books",
                fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // ======================================================
    // MAP
    // ======================================================

    private static BookDto MapToDto(Book book)
    {
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
            UserId = null
        };
    }
}
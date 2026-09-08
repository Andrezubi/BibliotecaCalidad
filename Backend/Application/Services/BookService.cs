using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Models;
using Backend.Domain.Interfaces;
using Backend.Domain.Validators;

namespace Backend.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
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

    public async Task<BookDto> CreateAsync(CreateBookDto dto)
    {
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

        await _bookRepository.AddAsync(book);

        return MapToDto(book);
    }

    public async Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(id);

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

        await _bookRepository.UpdateAsync(book);

        return MapToDto(book);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null || !book.IsActive)
            return false;

        await _bookRepository.DeleteAsync(book);

        return true;
    }
    public async Task<IEnumerable<BookDto>> SearchAsync(
    string? title = null,
    string? author = null,
    string? category = null,
    string? isbn = null,
    string? publisher = null)
    {
        var books = await _bookRepository.SearchAsync(
            title,
            author,
            category,
            isbn,
            publisher);

        return books.Select(MapToDto);
    }
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
            IsActive = book.IsActive,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt,
            UserId = null
        };
    }
    public async Task<IEnumerable<BookDto>> GetAvailableAsync()
    {
        var books = await _bookRepository.GetAvailableAsync();
        return books.Select(MapToDto);
    }
}
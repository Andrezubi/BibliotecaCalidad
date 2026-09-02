using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Entities;

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

        return books.Select(book => new BookDto
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
            UserId = book.UserId
        });
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null)
            return null;

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
            UserId = book.UserId
        };
    }

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

        await _bookRepository.AddAsync(book);

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
            UserId = book.UserId
        };
    }

    public async Task<BookDto?> UpdateAsync(int id, UpdateBookDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null)
            return null;

        book.Title = dto.Title;
        book.EditionNumber = dto.EditionNumber;
        book.ISBN = dto.ISBN;
        book.PublicationYear = dto.PublicationYear;
        book.Publisher = dto.Publisher;
        book.PageCount = dto.PageCount;
        book.Description = dto.Description;
        book.UserId = dto.UserId;
        book.UpdatedAt = DateTime.Now;

        await _bookRepository.UpdateAsync(book);

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
            UserId = book.UserId
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null)
            return false;

        book.IsActive = false;
        book.UpdatedAt = DateTime.Now;

        await _bookRepository.UpdateAsync(book);

        return true;
    }
}
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;

namespace Backend.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IEnumerable<BookDto>> GetAllBooksAsync(bool onlyAvailable = false)
    {
        var books = await _bookRepository.GetAllAsync();

        if (onlyAvailable)
        {
            books = books.Where(b => b.Copies != null && b.Copies.Any(c => c.Status == "Available"));
        }

        return books.Select(b => new BookDto
        {
            Id = b.Id,
            Title = b.Title,
            Isbn = b.Isbn,
            IsAvailable = b.Copies != null && b.Copies.Any(c => c.Status == "Available")
        });
    }

    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        var books = await _bookRepository.GetAllAsync();
        var book = books.FirstOrDefault(b => b.Id == id);

        if (book == null)
        {
            return null;
        }

        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            IsAvailable = book.Copies != null && book.Copies.Any(c => c.Status == "Available")
        };
    }
}
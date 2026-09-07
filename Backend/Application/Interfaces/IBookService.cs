using Backend.Application.DTOs;

namespace Backend.Application.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllBooksAsync(bool onlyAvailable = false);
    Task<BookDto?> GetBookByIdAsync(int id);
}
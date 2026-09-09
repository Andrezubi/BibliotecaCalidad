using Backend.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllAsync();

    Task<BookDto?> GetByIdAsync(int id);

    Task<BookDto> CreateAsync(CreateBookDto dto);

    Task<BookDto?> UpdateAsync(int id, UpdateBookDto dto);

    Task<bool> DeleteAsync(int id);

    Task<IEnumerable<BookDto>> GetAvailableAsync();

    Task<string?> AddCopyAsync(int bookId, string internalCode);

    Task<IEnumerable<BookDto>> SearchAsync(string? phrase = null);
}
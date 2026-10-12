using Backend.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllAsync();

    Task<BookDto?> GetByIdAsync(uint id);

    Task<BookDto> CreateAsync(CreateBookDto dto);

    Task<BookDto?> UpdateAsync(uint id, UpdateBookDto dto);

    Task<bool> DeleteAsync(uint id);

    Task<IEnumerable<BookDto>> GetAvailableAsync();

    Task<string?> AddCopyAsync(uint bookId, string internalCode);

    Task<IEnumerable<BookDto>> SearchAsync(string? phrase = null);
}
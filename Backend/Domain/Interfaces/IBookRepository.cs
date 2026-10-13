using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book, uint>
{
    Task<bool> ExistsByIsbnAsync(string isbn);

    Task<IEnumerable<Book>> SearchAsync(
        string? phrase = null);

    Task<List<uint>> GetAuthorIdsAsync(uint bookId);

    Task<List<byte>> GetCategoryIdsAsync(uint bookId);

    Task UpdateAuthorsAsync(
        uint bookId,
        IEnumerable<uint> authorIds,
        ushort? userId);

    Task UpdateCategoriesAsync(
        uint bookId,
        IEnumerable<byte> categoryIds,
        ushort? userId);


    Task<IEnumerable<Book>> GetAvailableAsync();

    Task<int> CountAvailableCopiesAsync(uint bookId);

    Task<bool> InternalCodeExistsAsync(string internalCode);

    Task AddCopyAsync(Copy copy);
}
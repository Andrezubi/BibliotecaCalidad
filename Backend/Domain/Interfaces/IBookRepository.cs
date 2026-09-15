using Backend.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book>
{
    Task<bool> ExistsByIsbnAsync(string isbn);

    Task<IEnumerable<Book>> SearchAsync(
        string? phrase = null);

    Task<List<int>> GetAuthorIdsAsync(int bookId);

    Task<List<int>> GetCategoryIdsAsync(int bookId);

    Task UpdateAuthorsAsync(
        int bookId,
        IEnumerable<int> authorIds,
        int? userId);

    Task UpdateCategoriesAsync(
        int bookId,
        IEnumerable<int> categoryIds,
        int? userId);


    Task<IEnumerable<Book>> GetAvailableAsync();

    Task<int> CountAvailableCopiesAsync(int bookId);

    Task<bool> InternalCodeExistsAsync(string internalCode);

    Task AddCopyAsync(Copy copy);
}
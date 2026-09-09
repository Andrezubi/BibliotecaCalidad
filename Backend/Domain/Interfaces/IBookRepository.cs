using Backend.Domain.Models;

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
}
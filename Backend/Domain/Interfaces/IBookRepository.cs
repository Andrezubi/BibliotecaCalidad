using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book>
{
    Task<bool> ExistsByIsbnAsync(string isbn);
    Task<IEnumerable<Book>> SearchAsync(
        string? title = null,
        string? author = null,
        string? category = null,
        string? isbn = null,
        string? publisher = null);
    Task<IEnumerable<Book>> GetAvailableAsync();
    Task<bool> InternalCodeExistsAsync(string internalCode);
    Task AddCopyAsync(Copy copy);
    Task<int> CountAvailableCopiesAsync(int bookId);
}
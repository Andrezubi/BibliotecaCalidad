using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book>
{
    Task<bool> ExistsByIsbnAsync(string isbn);
    Task<IEnumerable<Book>> SearchAsync(
        string? phrase = null);
}
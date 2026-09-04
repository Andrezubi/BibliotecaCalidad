using Backend.Domain.Entities;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book>
{
    Task<bool> ExistsByIsbnAsync(string isbn);
}
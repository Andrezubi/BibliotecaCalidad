using Backend.Domain.Entities;

namespace Backend.Domain.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();

    Task<Book?> GetByIdAsync(int id);

    Task<bool> ExistsByIsbnAsync(string isbn);

    Task AddAsync(Book book);

    Task UpdateAsync(Book book);

    Task DeleteAsync(Book book);
}
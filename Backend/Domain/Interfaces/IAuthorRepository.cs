using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface IAuthorRepository : IBaseRepository<Author>
{
    Task<bool> ExistsAsync(
        string firstName,
        string lastName);

    Task<IEnumerable<Author>> GetAllAsync();

    Task DeleteAsync(Author author);

    Task<bool> ExistsActiveByIdAsync(int id);

    Task AddBookAuthorAsync(Bookauthor bookAuthor);
}
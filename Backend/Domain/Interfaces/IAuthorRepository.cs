using Backend.Models;

namespace Backend.Domain.Interfaces;

public interface IAuthorRepository : IBaseRepository<Author, uint>
{
    Task<bool> ExistsAsync(
        string firstName,
        string lastName);

    Task<bool> ExistsActiveByIdAsync(uint id);

    Task AddBookAuthorAsync(Bookauthor bookAuthor);
}
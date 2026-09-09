using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class AuthorRepository
    : BaseRepository<Author>,
      IAuthorRepository
{
    public AuthorRepository(
        LibraryDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(author => author.IsActive)
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(
        string firstName,
        string lastName)
    {
        firstName = firstName.Trim();
        lastName = lastName.Trim();

        return await _dbSet.AnyAsync(author =>
            author.IsActive &&
            author.FirstName == firstName &&
            author.LastName == lastName);
    }

    public async Task<bool> ExistsActiveByIdAsync(int id)
    {
        return await _dbSet.AnyAsync(author =>
            author.Id == id &&
            author.IsActive);
    }

    public async Task DeleteAsync(Author author)
    {
        author.IsActive = false;
        author.UpdatedAt = DateTime.Now;

        await UpdateAsync(author);
    }

    public async Task AddBookAuthorAsync(
        Bookauthor bookAuthor)
    {
        await _context.Bookauthors.AddAsync(bookAuthor);
        await _context.SaveChangesAsync();
    }
}
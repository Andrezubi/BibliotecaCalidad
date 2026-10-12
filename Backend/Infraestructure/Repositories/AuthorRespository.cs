using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class AuthorRepository
    : BaseRepository<Author, uint>,
      IAuthorRepository
{
    public AuthorRepository(
        LibraryDbcontext context)
        : base(context)
    {
    }

    public new async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(author => author.IsActive == true)
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
            author.IsActive == true &&
            author.FirstName == firstName &&
            author.LastName == lastName);
    }

    public async Task<bool> ExistsActiveByIdAsync(uint id)
    {
        return await _dbSet.AnyAsync(author =>
            author.Id == id &&
            author.IsActive == true);
    }

    public new async Task DeleteAsync(Author author)
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
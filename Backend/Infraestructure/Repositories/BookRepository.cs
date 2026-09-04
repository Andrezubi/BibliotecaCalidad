using Backend.Domain.Models;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class BookRepository : BaseRepository<Book>, IBookRepository
{
    public BookRepository(LibraryDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(book => book.IsActive)
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn)
    {
        return await _dbSet
            .AnyAsync(book =>
                book.ISBN == isbn &&
                book.IsActive);
    }

    public async Task DeleteAsync(Book book)
    {
        book.IsActive = false;
        book.UpdatedAt = DateTime.Now;

        await UpdateAsync(book);
    }
}
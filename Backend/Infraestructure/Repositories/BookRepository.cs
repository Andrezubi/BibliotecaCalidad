using Backend.Domain.Entities;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Persistence;
using Backend.EntityFrameworkCore;

namespace LibraryBookCrud.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _context.Books
            .AsNoTracking()
            .Where(book => book.IsActive)
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books
            .FirstOrDefaultAsync(book => book.Id == id);
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn)
    {
        return await _context.Books
            .AnyAsync(book =>
                book.ISBN == isbn &&
                book.IsActive);
    }

    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Book book)
    {
        book.IsActive = false;
        book.UpdatedAt = DateTime.Now;

        _context.Books.Update(book);

        await _context.SaveChangesAsync();
    }
}
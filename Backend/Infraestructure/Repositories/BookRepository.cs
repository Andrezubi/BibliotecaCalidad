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
    public async Task<IEnumerable<Book>> SearchAsync(
        string? title = null,
        string? author = null,
        string? category = null,
        string? isbn = null,
        string? publisher = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(book => book.IsActive);

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(book =>
                EF.Functions.Like(book.Title, $"%{title}%"));
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            query = query.Where(book =>
                book.ISBN != null &&
                EF.Functions.Like(book.ISBN, $"%{isbn}%"));
        }

        if (!string.IsNullOrWhiteSpace(publisher))
        {
            query = query.Where(book =>
                book.Publisher != null &&
                EF.Functions.Like(book.Publisher, $"%{publisher}%"));
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(book =>
                
                book.Bookauthors.Any(ba =>
                    ba.IsActive &&
                    ba.Author.IsActive &&
                    (
                        EF.Functions.Like(ba.Author.FirstName, $"%{author}%") ||
                        EF.Functions.Like(ba.Author.LastName, $"%{author}%") ||
                        EF.Functions.Like(
                            ba.Author.FirstName + " " + ba.Author.LastName,
                            $"%{author}%")
                    )));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(book =>
                book.Bookcategories.Any(bc =>
                    bc.IsActive &&
                    bc.Category.IsActive &&
                    EF.Functions.Like(
                        bc.Category.Name,
                        $"%{category}%")));
        }

        return await query
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

}
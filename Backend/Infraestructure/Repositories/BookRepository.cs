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
    public async Task<IEnumerable<Book>> SearchAsync(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await GetAllAsync();
        }

        search = search.Trim();

        return await _dbSet
            .AsNoTracking()
            .Where(book =>
                book.IsActive &&
                (
                    EF.Functions.Like(book.Title, $"%{search}%") ||

                    (book.ISBN != null &&
                     EF.Functions.Like(book.ISBN, $"%{search}%")) ||

                    (book.Description != null &&
                     EF.Functions.Like(book.Description, $"%{search}%")) ||

                    (book.Publisher != null &&
                     EF.Functions.Like(book.Publisher, $"%{search}%")) ||

                    book.Bookauthors.Any(ba =>
                        ba.IsActive &&
                        ba.Author.IsActive &&
                        (
                            EF.Functions.Like(
                                ba.Author.FirstName,
                                $"%{search}%"
                            ) ||

                            EF.Functions.Like(
                                ba.Author.LastName,
                                $"%{search}%"
                            )
                        )
                    ) ||

                    book.Bookcategories.Any(bc =>
                        bc.IsActive &&
                        bc.Category.IsActive &&
                        EF.Functions.Like(
                            bc.Category.Name,
                            $"%{search}%"
                        )
                    )
                )
            )
            .OrderBy(book => book.Title)
            .ToListAsync();
    }
    public async Task<IEnumerable<Book>> GetAvailableAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(book =>
                book.IsActive &&
                book.Copies.Any(copy =>
                    copy.IsActive &&
                    copy.Status == "Available"))
            .OrderBy(book => book.Title)
            .ToListAsync();
    }
    public async Task<bool> InternalCodeExistsAsync(string internalCode)
    {
        return await _context.Copies
            .AnyAsync(c => c.InternalCode == internalCode);
    }

    public async Task AddCopyAsync(Copy copy)
    {
        await _context.Copies.AddAsync(copy);
        await _context.SaveChangesAsync();
    }
    public async Task<int> CountAvailableCopiesAsync(int bookId)
    {
        return await _context.Copies
            .CountAsync(c =>
                c.BookId == bookId &&
                c.IsActive &&
                c.Status == "Available");
    }

}
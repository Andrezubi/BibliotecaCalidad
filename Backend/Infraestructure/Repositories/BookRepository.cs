using Backend.Domain.Models;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class BookRepository
    : BaseRepository<Book>,
      IBookRepository
{
    public BookRepository(
        LibraryDbContext context)
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

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(book => book.Bookauthors)
            .Include(book => book.Bookcategories)
            .FirstOrDefaultAsync(book =>
                book.Id == id);
    }

    public async Task<bool> ExistsByIsbnAsync(
        string isbn)
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
        string? search)
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
                    EF.Functions.Like(
                        book.Title,
                        $"%{search}%")

                    ||

                    (book.ISBN != null &&
                     EF.Functions.Like(
                         book.ISBN,
                         $"%{search}%"))

                    ||

                    (book.Description != null &&
                     EF.Functions.Like(
                         book.Description,
                         $"%{search}%"))

                    ||

                    (book.Publisher != null &&
                     EF.Functions.Like(
                         book.Publisher,
                         $"%{search}%"))

                    ||

                    book.Bookauthors.Any(ba =>
                        ba.IsActive &&
                        ba.Author.IsActive &&
                        (
                            EF.Functions.Like(
                                ba.Author.FirstName,
                                $"%{search}%")

                            ||

                            EF.Functions.Like(
                                ba.Author.LastName,
                                $"%{search}%")
                        ))

                    ||

                    book.Bookcategories.Any(bc =>
                        bc.IsActive &&
                        bc.Category.IsActive &&
                        EF.Functions.Like(
                            bc.Category.Name,
                            $"%{search}%"))
                )
            )
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    public async Task<List<int>> GetAuthorIdsAsync(
        int bookId)
    {
        return await _context.Bookauthors
            .AsNoTracking()
            .Where(ba =>
                ba.BookId == bookId &&
                ba.IsActive &&
                ba.Author.IsActive)
            .Select(ba => ba.AuthorId)
            .ToListAsync();
    }

    public async Task<List<int>> GetCategoryIdsAsync(
        int bookId)
    {
        return await _context.Bookcategories
            .AsNoTracking()
            .Where(bc =>
                bc.BookId == bookId &&
                bc.IsActive &&
                bc.Category.IsActive)
            .Select(bc => bc.CategoryId)
            .ToListAsync();
    }

    public async Task UpdateAuthorsAsync(
        int bookId,
        IEnumerable<int> authorIds,
        int? userId)
    {
        var selectedIds =
            authorIds
                .Distinct()
                .ToHashSet();

        var relations =
            await _context.Bookauthors
                .Where(ba => ba.BookId == bookId)
                .ToListAsync();

        foreach (var relation in relations)
        {
            if (selectedIds.Contains(relation.AuthorId))
            {
                relation.IsActive = true;
                relation.UpdatedAt = DateTime.Now;
                relation.UserId = userId;
            }
            else
            {
                relation.IsActive = false;
                relation.UpdatedAt = DateTime.Now;
            }
        }

        var existingIds =
            relations
                .Select(r => r.AuthorId)
                .ToHashSet();

        foreach (var authorId in selectedIds)
        {
            if (!existingIds.Contains(authorId))
            {
                await _context.Bookauthors.AddAsync(
                    new Bookauthor
                    {
                        BookId = bookId,
                        AuthorId = authorId,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UserId = userId
                    });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateCategoriesAsync(
        int bookId,
        IEnumerable<int> categoryIds,
        int? userId)
    {
        var selectedIds =
            categoryIds
                .Distinct()
                .ToHashSet();

        var relations =
            await _context.Bookcategories
                .Where(bc => bc.BookId == bookId)
                .ToListAsync();

        foreach (var relation in relations)
        {
            if (selectedIds.Contains(relation.CategoryId))
            {
                relation.IsActive = true;
                relation.UpdatedAt = DateTime.Now;
                relation.UserId = userId;
            }
            else
            {
                relation.IsActive = false;
                relation.UpdatedAt = DateTime.Now;
            }
        }

        var existingIds =
            relations
                .Select(r => r.CategoryId)
                .ToHashSet();

        foreach (var categoryId in selectedIds)
        {
            if (!existingIds.Contains(categoryId))
            {
                await _context.Bookcategories.AddAsync(
                    new Bookcategory
                    {
                        BookId = bookId,
                        CategoryId = categoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UserId = userId
                    });
            }
        }

        await _context.SaveChangesAsync();
    }
}
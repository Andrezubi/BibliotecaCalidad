using Backend.Domain.Interfaces;
using Backend.Domain.Models;
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

    // ============================================================
    // OBTENER TODOS
    // ============================================================

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(book => book.IsActive)
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    // ============================================================
    // OBTENER POR ID
    // ============================================================

    public async Task<Book?> GetByIdAsync(
        int id)
    {
        return await _dbSet
            .Include(book => book.Bookauthors)
            .Include(book => book.Bookcategories)
            .FirstOrDefaultAsync(book =>
                book.Id == id);
    }

    // ============================================================
    // VALIDAR ISBN
    // ============================================================

    public async Task<bool> ExistsByIsbnAsync(
        string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return false;
        }

        return await _dbSet
            .AnyAsync(book =>
                book.ISBN == isbn &&
                book.IsActive);
    }

    // ============================================================
    // ELIMINACIÓN LÓGICA
    // ============================================================

    public async Task DeleteAsync(
        Book book)
    {
        book.IsActive = false;
        book.UpdatedAt = DateTime.Now;

        await UpdateAsync(book);
    }

    // ============================================================
    // BUSCAR
    // ============================================================

    public async Task<IEnumerable<Book>> SearchAsync(
        string? phrase = null)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return await GetAllAsync();
        }

        phrase = phrase.Trim();

        return await _dbSet
            .AsNoTracking()
            .Where(book =>
                book.IsActive &&
                (
                    EF.Functions.Like(
                        book.Title,
                        $"%{phrase}%")

                    ||

                    (
                        book.ISBN != null &&
                        EF.Functions.Like(
                            book.ISBN,
                            $"%{phrase}%")
                    )

                    ||

                    (
                        book.Description != null &&
                        EF.Functions.Like(
                            book.Description,
                            $"%{phrase}%")
                    )

                    ||

                    (
                        book.Publisher != null &&
                        EF.Functions.Like(
                            book.Publisher,
                            $"%{phrase}%")
                    )

                    ||

                    book.Bookauthors.Any(ba =>
                        ba.IsActive &&
                        ba.Author.IsActive &&
                        (
                            EF.Functions.Like(
                                ba.Author.FirstName,
                                $"%{phrase}%")

                            ||

                            EF.Functions.Like(
                                ba.Author.LastName,
                                $"%{phrase}%")
                        ))

                    ||

                    book.Bookcategories.Any(bc =>
                        bc.IsActive &&
                        bc.Category.IsActive &&
                        EF.Functions.Like(
                            bc.Category.Name,
                            $"%{phrase}%"))
                )
            )
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    // ============================================================
    // OBTENER LIBROS DISPONIBLES
    // ============================================================

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

    // ============================================================
    // VALIDAR CÓDIGO INTERNO DE COPIA
    // ============================================================

    public async Task<bool> InternalCodeExistsAsync(
        string internalCode)
    {
        return await _context.Copies
            .AnyAsync(copy =>
                copy.InternalCode == internalCode);
    }

    // ============================================================
    // AGREGAR COPIA
    // ============================================================

    public async Task AddCopyAsync(
        Copy copy)
    {
        await _context.Copies.AddAsync(copy);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // CONTAR COPIAS DISPONIBLES
    // ============================================================

    public async Task<int> CountAvailableCopiesAsync(
        int bookId)
    {
        return await _context.Copies
            .CountAsync(copy =>
                copy.BookId == bookId &&
                copy.IsActive &&
                copy.Status == "Available");
    }

    // ============================================================
    // OBTENER AUTORES ACTIVOS DEL LIBRO
    // ============================================================

    public async Task<List<int>> GetAuthorIdsAsync(
        int bookId)
    {
        return await _context.Bookauthors
            .AsNoTracking()
            .Where(bookAuthor =>
                bookAuthor.BookId == bookId &&
                bookAuthor.IsActive &&
                bookAuthor.Author.IsActive)
            .Select(bookAuthor =>
                bookAuthor.AuthorId)
            .ToListAsync();
    }

    // ============================================================
    // OBTENER CATEGORÍAS ACTIVAS DEL LIBRO
    // ============================================================

    public async Task<List<int>> GetCategoryIdsAsync(
        int bookId)
    {
        return await _context.Bookcategories
            .AsNoTracking()
            .Where(bookCategory =>
                bookCategory.BookId == bookId &&
                bookCategory.IsActive &&
                bookCategory.Category.IsActive)
            .Select(bookCategory =>
                bookCategory.CategoryId)
            .ToListAsync();
    }

    // ============================================================
    // ACTUALIZAR AUTORES
    // ============================================================

    public async Task UpdateAuthorsAsync(
        int bookId,
        IEnumerable<int> authorIds,
        int? userId)
    {
        var selectedIds = authorIds
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        var relations = await _context.Bookauthors
            .Where(bookAuthor =>
                bookAuthor.BookId == bookId)
            .ToListAsync();

        var now = DateTime.Now;

        foreach (var relation in relations)
        {
            if (selectedIds.Contains(
                    relation.AuthorId))
            {
                relation.IsActive = true;
                relation.UpdatedAt = now;
                relation.UserId = userId;
            }
            else
            {
                relation.IsActive = false;
                relation.UpdatedAt = now;
            }
        }

        var existingIds = relations
            .Select(relation =>
                relation.AuthorId)
            .ToHashSet();

        foreach (var authorId in selectedIds)
        {
            if (existingIds.Contains(authorId))
            {
                continue;
            }

            await _context.Bookauthors.AddAsync(
                new Bookauthor
                {
                    BookId = bookId,
                    AuthorId = authorId,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = null,
                    UserId = userId
                });
        }

        await _context.SaveChangesAsync();
    }

    // ============================================================
    // ACTUALIZAR CATEGORÍAS
    // ============================================================

    public async Task UpdateCategoriesAsync(
        int bookId,
        IEnumerable<int> categoryIds,
        int? userId)
    {
        var selectedIds = categoryIds
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        var relations = await _context.Bookcategories
            .Where(bookCategory =>
                bookCategory.BookId == bookId)
            .ToListAsync();

        var now = DateTime.Now;

        foreach (var relation in relations)
        {
            if (selectedIds.Contains(
                    relation.CategoryId))
            {
                relation.IsActive = true;
                relation.UpdatedAt = now;
                relation.UserId = userId;
            }
            else
            {
                relation.IsActive = false;
                relation.UpdatedAt = now;
            }
        }

        var existingIds = relations
            .Select(relation =>
                relation.CategoryId)
            .ToHashSet();

        foreach (var categoryId in selectedIds)
        {
            if (existingIds.Contains(categoryId))
            {
                continue;
            }

            await _context.Bookcategories.AddAsync(
                new Bookcategory
                {
                    BookId = bookId,
                    CategoryId = categoryId,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = null,
                    UserId = userId
                });
        }

        await _context.SaveChangesAsync();
    }
}
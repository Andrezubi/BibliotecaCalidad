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

                    (
                        book.ISBN != null &&
                        EF.Functions.Like(
                            book.ISBN,
                            $"%{search}%")
                    )

                    ||

                    (
                        book.Description != null &&
                        EF.Functions.Like(
                            book.Description,
                            $"%{search}%")
                    )

                    ||

                    (
                        book.Publisher != null &&
                        EF.Functions.Like(
                            book.Publisher,
                            $"%{search}%")
                    )

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

    // ============================================================
    // OBTENER AUTORES ACTIVOS DEL LIBRO
    // ============================================================

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

    // ============================================================
    // OBTENER CATEGORÍAS ACTIVAS DEL LIBRO
    // ============================================================

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

        var relations =
            await _context.Bookauthors
                .Where(ba =>
                    ba.BookId == bookId)
                .ToListAsync();

        var now = DateTime.Now;

        // --------------------------------------------------------
        // ACTIVAR / DESACTIVAR RELACIONES EXISTENTES
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // OBTENER IDS YA EXISTENTES
        // --------------------------------------------------------

        var existingIds =
            relations
                .Select(r => r.AuthorId)
                .ToHashSet();

        // --------------------------------------------------------
        // AGREGAR NUEVAS RELACIONES
        // --------------------------------------------------------

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

        var relations =
            await _context.Bookcategories
                .Where(bc =>
                    bc.BookId == bookId)
                .ToListAsync();

        var now = DateTime.Now;

        // --------------------------------------------------------
        // ACTIVAR / DESACTIVAR RELACIONES EXISTENTES
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // OBTENER IDS YA EXISTENTES
        // --------------------------------------------------------

        var existingIds =
            relations
                .Select(r => r.CategoryId)
                .ToHashSet();

        // --------------------------------------------------------
        // AGREGAR NUEVAS RELACIONES
        // --------------------------------------------------------

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
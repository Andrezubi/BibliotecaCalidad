using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class BookRepository
    : BaseRepository<Book, uint>,
      IBookRepository
{
    public BookRepository(
        LibraryDbcontext context)
        : base(context)
    {
    }

    // ============================================================
    // OBTENER TODOS
    // ============================================================

    public new async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(book => book.IsActive == true)
            .OrderBy(book => book.Title)
            .ToListAsync();
    }

    // ============================================================
    // OBTENER POR ID
    // ============================================================

    public new async Task<Book?> GetByIdAsync(
        uint id)
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
                book.Isbn == isbn &&
                book.IsActive == true);
    }

    // ============================================================
    // ELIMINACIÓN LÓGICA
    // ============================================================

    public new async Task DeleteAsync(
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
                book.IsActive == true &&
                (
                    EF.Functions.Like(
                        book.Title,
                        $"%{phrase}%")

                    ||

                    (
                        book.Isbn != null &&
                        EF.Functions.Like(
                            book.Isbn,
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
                            book.Publisher.Name,
                            $"%{phrase}%")
                    )

                    ||

                    book.Bookauthors.Any(ba =>
                        ba.Author.IsActive == true &&
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
                        bc.Category.IsActive == true &&
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
                book.IsActive == true &&
                book.Copies.Any(copy =>
                    copy.StatusId == 1 && // Available
                    copy.Status.Name == "Available"))
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
        uint bookId)
    {
        return await _context.Copies
            .CountAsync(copy =>
                copy.BookId == bookId &&
                copy.StatusId == 1 && // Available
                copy.Status.Name == "Available");
    }

    // ============================================================
    // OBTENER AUTORES ACTIVOS DEL LIBRO
    // ============================================================

    public async Task<List<uint>> GetAuthorIdsAsync(
        uint bookId)
    {
        return await _context.Bookauthors
            .AsNoTracking()
            .Where(bookAuthor =>
                bookAuthor.BookId == bookId &&
                bookAuthor.Author.IsActive == true)
            .Select(bookAuthor =>
                bookAuthor.AuthorId)
            .ToListAsync();
    }

    // ============================================================
    // OBTENER CATEGORÍAS ACTIVAS DEL LIBRO
    // ============================================================

    public async Task<List<byte>> GetCategoryIdsAsync(
        uint bookId)
    {
        return await _context.Bookcategories
            .AsNoTracking()
            .Where(bookCategory =>
                bookCategory.BookId == bookId &&
                bookCategory.Category.IsActive == true)
            .Select(bookCategory =>
                bookCategory.CategoryId)
            .ToListAsync();
    }

    // ============================================================
    // ACTUALIZAR AUTORES
    // ============================================================

    public async Task UpdateAuthorsAsync(
        uint bookId,
        IEnumerable<uint> authorIds,
        ushort? userId)
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
                relation.UpdatedAt = now;
                relation.UserId = userId;
            }
            else
            {
                relation.UpdatedAt = now;
                // Soft delete by removing the relation
                _context.Bookauthors.Remove(relation);
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
        uint bookId,
        IEnumerable<byte> categoryIds,
        ushort? userId)
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
                relation.UpdatedAt = now;
                relation.UserId = userId;
            }
            else
            {
                relation.UpdatedAt = now;
                // Soft delete by removing the relation
                _context.Bookcategories.Remove(relation);
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
                    CreatedAt = now,
                    UpdatedAt = null,
                    UserId = userId
                });
        }

        await _context.SaveChangesAsync();
    }
}
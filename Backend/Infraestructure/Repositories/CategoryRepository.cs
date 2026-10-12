using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class CategoryRepository
    : BaseRepository<Category, byte>,
      ICategoryRepository
{
    public CategoryRepository(
        LibraryDbcontext context)
        : base(context)
    {
    }

    public new async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(category => category.IsActive == true)
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        name = name.Trim();

        return await _dbSet.AnyAsync(category =>
            category.IsActive == true &&
            category.Name == name);
    }

    public async Task<bool> ExistsActiveByIdAsync(byte id)
    {
        return await _dbSet.AnyAsync(category =>
            category.Id == id &&
            category.IsActive == true);
    }

    public new async Task DeleteAsync(Category category)
    {
        category.IsActive = false;
        category.UpdatedAt = DateTime.Now;

        await UpdateAsync(category);
    }

    public async Task AddBookCategoryAsync(
        Bookcategory bookCategory)
    {
        await _context.Bookcategories.AddAsync(bookCategory);
        await _context.SaveChangesAsync();
    }
}
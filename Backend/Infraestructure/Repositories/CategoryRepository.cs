using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class CategoryRepository
    : BaseRepository<Category>,
      ICategoryRepository
{
    public CategoryRepository(
        LibraryDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        name = name.Trim();

        return await _dbSet.AnyAsync(category =>
            category.IsActive &&
            category.Name == name);
    }

    public async Task<bool> ExistsActiveByIdAsync(int id)
    {
        return await _dbSet.AnyAsync(category =>
            category.Id == id &&
            category.IsActive);
    }

    public async Task DeleteAsync(Category category)
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
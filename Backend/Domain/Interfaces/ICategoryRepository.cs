using Backend.Models;

namespace Backend.Domain.Interfaces;

public interface ICategoryRepository : IBaseRepository<Category, byte>
{
    Task<bool> ExistsAsync(string name);

    Task<bool> ExistsActiveByIdAsync(byte id);

    Task AddBookCategoryAsync(Bookcategory bookCategory);
}
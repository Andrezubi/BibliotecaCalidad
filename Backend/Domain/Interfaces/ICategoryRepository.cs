using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<bool> ExistsAsync(string name);

    Task<bool> ExistsActiveByIdAsync(int id);

    Task AddBookCategoryAsync(Bookcategory bookCategory);
}
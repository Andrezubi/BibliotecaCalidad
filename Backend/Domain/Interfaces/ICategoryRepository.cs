using Backend.Domain.Models;

namespace Backend.Domain.Interfaces
{
    public interface ICategoryRepository:IBaseRepository<Category>
    {
        Task<bool> ExistsAsync(string name);
        Task<IEnumerable<Category>> GetAllAsync();
        Task DeleteAsync(Category category);
    }
}

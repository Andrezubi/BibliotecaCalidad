using Backend.Application.DTOs;
using Backend.Domain.Models;

namespace Backend.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();

        Task<Category?> GetByIdAsync(int id);

        Task<Category> CreateAsync(
            CreateCategoryDto dto);

        Task<Category?> UpdateAsync(
            int id,
            CreateCategoryDto dto);

        Task<bool> DeleteAsync(int id);
    }
}

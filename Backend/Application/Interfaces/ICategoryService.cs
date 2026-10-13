using Backend.Application.DTOs;
using Backend.Models;

namespace Backend.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();

        Task<Category?> GetByIdAsync(byte id);

        Task<Category> CreateAsync(
            CreateCategoryDto dto);

        Task<Category?> UpdateAsync(
            byte id,
            CreateCategoryDto dto);

        Task<bool> DeleteAsync(byte id);
    }
}
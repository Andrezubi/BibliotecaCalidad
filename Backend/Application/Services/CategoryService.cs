using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

namespace Backend.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(
            ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            var categories =
                await _categoryRepository.GetAllAsync();

            return categories.Select(MapToDto);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            var category =
                await _categoryRepository.GetByIdAsync(id);

            if (category == null || !category.IsActive)
                return null;

            return MapToDto(category);
        }

        public async Task<Category> CreateAsync(
            CreateCategoryDto dto)
        {
            var exists =
                await _categoryRepository.ExistsAsync(dto.Name);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe una categoría activa con ese nombre.");
            }

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                UserId = dto.UserId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _categoryRepository.AddAsync(category);

            return MapToDto(category);
        }

        public async Task<Category?> UpdateAsync(
            int id,
            CreateCategoryDto dto)
        {
            var category =
                await _categoryRepository.GetByIdAsync(id);

            if (category == null || !category.IsActive)
                return null;

            if (category.Name != dto.Name)
            {
                var exists =
                    await _categoryRepository.ExistsAsync(dto.Name);

                if (exists)
                {
                    throw new InvalidOperationException(
                        "Ya existe una categoría activa con ese nombre.");
                }
            }

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.UserId = dto.UserId;
            category.UpdatedAt = DateTime.Now;

            await _categoryRepository.UpdateAsync(category);

            return MapToDto(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category =
                await _categoryRepository.GetByIdAsync(id);

            if (category == null || !category.IsActive)
                return false;

            await _categoryRepository.DeleteAsync(category);

            return true;
        }

        private static Category MapToDto(
            Category category)
        {
            return new Category
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}

using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Models;

namespace Backend.Application.Services;

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
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<Category?> GetByIdAsync(byte id)
    {
        var category =
            await _categoryRepository
                .GetByIdAsync(id);

        if (category == null ||
            category.IsActive != true)
        {
            return null;
        }

        return category;
    }

    public async Task<Category> CreateAsync(
        CreateCategoryDto dto)
    {
        var name =
            dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre de la categoría es obligatorio.");
        }

        var exists =
            await _categoryRepository
                .ExistsAsync(name);

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe una categoría activa con ese nombre.");
        }

        var category = new Category
        {
            Name = name,
            Description =
                string.IsNullOrWhiteSpace(
                    dto.Description)
                    ? null
                    : dto.Description.Trim(),

            UserId = dto.UserId != null ? (ushort)dto.UserId.Value : null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _categoryRepository.AddAsync(category);

        return category;
    }

    public async Task<Category?> UpdateAsync(
        byte id,
        CreateCategoryDto dto)
    {
        var category =
            await _categoryRepository
                .GetByIdAsync(id);

        if (category == null ||
            category.IsActive != true)
        {
            return null;
        }

        var name =
            dto.Name.Trim();

        if (category.Name != name)
        {
            var exists =
                await _categoryRepository
                    .ExistsAsync(name);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe una categoría activa con ese nombre.");
            }
        }

        category.Name = name;

        category.Description =
            string.IsNullOrWhiteSpace(
                dto.Description)
                ? null
                : dto.Description.Trim();

        category.UserId = dto.UserId != null ? (ushort)dto.UserId.Value : null;
        category.UpdatedAt = DateTime.Now;

        await _categoryRepository
            .UpdateAsync(category);

        return category;
    }

    public async Task<bool> DeleteAsync(byte id)
    {
        var category =
            await _categoryRepository
                .GetByIdAsync(id);

        if (category == null ||
            category.IsActive != true)
        {
            return false;
        }

        await _categoryRepository
            .DeleteAsync(category);

        return true;
    }
}
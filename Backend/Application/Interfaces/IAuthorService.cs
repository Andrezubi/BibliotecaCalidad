using Backend.Application.DTOs;
using Backend.Models;

namespace Backend.Application.Interfaces
{
    public interface IAuthorService
    {
        Task<IEnumerable<Author>> GetAllAsync();

        Task<Author?> GetByIdAsync(uint id);

        Task<Author> CreateAsync(CreateAuthorDto dto);

        Task<Author?> UpdateAsync(
            uint id,
            CreateAuthorDto dto);

        Task<bool> DeleteAsync(uint id);
    }
}
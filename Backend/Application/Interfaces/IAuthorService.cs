using Backend.Application.DTOs;
using Backend.Domain.Models;

namespace Backend.Application.Interfaces
{
    public interface IAuthorService
    {
        Task<IEnumerable<Author>> GetAllAsync();

        Task<Author?> GetByIdAsync(int id);

        Task<Author> CreateAsync(CreateAuthorDto dto);

        Task<Author?> UpdateAsync(
            int id,
            CreateAuthorDto dto);

        Task<bool> DeleteAsync(int id);
    }
}

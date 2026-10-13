using Backend.Application.DTOs;
using Backend.Models;

namespace Backend.Application.Interfaces
{
    public interface IPublisherService
    {
        Task<IEnumerable<Publisher>> GetAllAsync();

        Task<Publisher?> GetByIdAsync(uint id);

        Task<Publisher> CreateAsync(CreatePublisherDto dto);

        Task<Publisher?> UpdateAsync(uint id, CreatePublisherDto dto);

        Task<bool> DeleteAsync(uint id);
    }
}
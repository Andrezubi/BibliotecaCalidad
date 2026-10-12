using Backend.Models;

namespace Backend.Domain.Interfaces;

public interface IPublisherRepository : IBaseRepository<Publisher, uint>
{
    Task<bool> ExistsAsync(string name);

    Task<bool> ExistsActiveByIdAsync(uint id);
}
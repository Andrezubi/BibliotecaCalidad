using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class PublisherRepository
    : BaseRepository<Publisher, uint>,
      IPublisherRepository
{
    public PublisherRepository(
        LibraryDbcontext context)
        : base(context)
    {
    }

    public new async Task<IEnumerable<Publisher>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(publisher => publisher.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        name = name.Trim();

        return await _dbSet.AnyAsync(publisher =>
            publisher.Name == name);
    }

    public async Task<bool> ExistsActiveByIdAsync(uint id)
    {
        return await _dbSet.AnyAsync(publisher =>
            publisher.Id == id);
    }

    public new async Task DeleteAsync(Publisher publisher)
    {
        _dbSet.Remove(publisher);
        await _context.SaveChangesAsync();
    }
}
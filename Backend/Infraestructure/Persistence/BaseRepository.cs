using Backend.Domain.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend.Infraestructure.Persistence;

public class BaseRepository<T, TKey> : IBaseRepository<T, TKey>
    where T : class
{
    protected readonly LibraryDbcontext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(LibraryDbcontext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));

        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
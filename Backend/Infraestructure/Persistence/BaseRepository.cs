using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Backend.Infrastructure.Persistence;
namespace Backend.Infrastructure.Persistence;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly LibraryDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(LibraryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

   // public async Task<T?> GetByIdAsync(int id)
    //{
    //    return await _dbSet.FindAsync(id);
    //}

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}

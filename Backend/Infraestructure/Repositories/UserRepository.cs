using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(LibraryDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username);
    }

    public async Task<bool> CiExistsAsync(
        int ci,
        string? complement)
    {
        var normalizedComplement = complement ?? string.Empty;

        return await _context.Users
            .AnyAsync(u =>
                u.Ci == ci &&
                (u.Complement ?? string.Empty) == normalizedComplement
            );
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
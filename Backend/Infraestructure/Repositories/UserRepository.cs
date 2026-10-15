using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LibraryDbcontext _context;

    public UserRepository(LibraryDbcontext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
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

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetReadersAsync()
    {
        return await _context.Users
            .Where(u => u.Role == "User" && (u.IsActive ?? false)
                && u.IsLibraryComputerUser == false
                && !u.LoanBorrowers.Any(l => l.Status == "Active"))
            .OrderBy(u => u.FirstName)
            .ToListAsync();
    }
}
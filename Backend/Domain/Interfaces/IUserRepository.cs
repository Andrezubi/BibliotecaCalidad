using Backend.Models;

namespace Backend.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<bool> UsernameExistsAsync(string username);

    Task<bool> CiExistsAsync(int ci, string? complement);

    Task AddAsync(User user);

    Task SaveChangesAsync();
}
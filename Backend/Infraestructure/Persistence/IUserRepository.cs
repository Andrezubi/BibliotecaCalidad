using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<bool> UsernameExistsAsync(string username);

    Task<bool> CiExistsAsync(int ci, string? complement);

    Task<Role?> GetRoleByNameAsync(string roleName);

    Task AddAsync(User user);

    Task SaveChangesAsync();
}
using Backend.Domain.Models;

namespace Backend.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByUsernameAsync(string username);

        Task<bool> ExistsByCIAsync(int ci, string? complement);

        Task<int?> GetRoleIdByNameAsync(string roleName);

        Task<int> CreateAsync(User user);
    }
}
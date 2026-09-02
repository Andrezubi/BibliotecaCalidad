using Backend.Domain.Models;

namespace Backend.Application.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(User user, string password);

        bool VerifyPassword(
            User user,
            string hashedPassword,
            string password
        );
    }
}
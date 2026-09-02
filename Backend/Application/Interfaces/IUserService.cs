using Backend.Application.DTOs;

namespace Backend.Application.Interfaces
{
    public interface IUserService
    {
        Task<int> RegisterAsync(RegisterUserRequest request);
    }
}
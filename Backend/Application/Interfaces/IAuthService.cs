using Backend.Application.DTOs;

namespace Backend.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
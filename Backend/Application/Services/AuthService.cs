using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;

namespace Backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException(
                "El usuario y la contraseña son obligatorios."
            );
        }

        var username = request.Username.Trim();

        var user = await _userRepository
            .GetByUsernameAsync(username);

        if (user is null)
        {
            throw new InvalidOperationException(
                "Usuario o contraseña incorrectos."
            );
        }

        if (user.IsActive != true ||
            user.Status != "Active")
        {
            throw new InvalidOperationException(
                "El usuario no se encuentra activo."
            );
        }

        var validPassword =
            _passwordService.VerifyPassword(
                request.Password,
                user.PasswordHash
            );

        if (!validPassword)
        {
            throw new InvalidOperationException(
                "Usuario o contraseña incorrectos."
            );
        }

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            Role = user.Role.Name
        };
    }
}
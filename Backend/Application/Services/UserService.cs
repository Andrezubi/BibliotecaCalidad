using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Models;

namespace Backend.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;

    public UserService(
        IUserRepository userRepository,
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public async Task<int> RegisterAsync(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new InvalidOperationException(
                "El nombre es obligatorio."
            );
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new InvalidOperationException(
                "El apellido es obligatorio."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new InvalidOperationException(
                "El nombre de usuario es obligatorio."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException(
                "La contraseña es obligatoria."
            );
        }

        if (request.Ci <= 0)
        {
            throw new InvalidOperationException(
                "El CI ingresado no es válido."
            );
        }

        var username = request.Username.Trim();

        var complement = request.Complement?.Trim() ?? string.Empty;

        if (await _userRepository.UsernameExistsAsync(username))
        {
            throw new InvalidOperationException(
                "El nombre de usuario ya está registrado."
            );
        }

        if (await _userRepository.CiExistsAsync(
                request.Ci,
                complement))
        {
            throw new InvalidOperationException(
                "El CI ingresado ya está registrado."
            );
        }

        var passwordHash =
            _passwordService.HashPassword(request.Password);

        var user = new User
        {
            Ci = request.Ci,
            Complement = complement,

            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),

            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),

            Username = username,

            PasswordHash = passwordHash,

            Role = "User",

            Status = "Active",

            CreatedAt = DateTime.Now
        };

        await _userRepository.AddAsync(user);

        await _userRepository.SaveChangesAsync();

        return user.Id;
    }

    public async Task<IEnumerable<Backend.Application.DTOs.UserListDto>> GetReadersAsync()
    {
        var users = await _userRepository.GetReadersAsync();

        return users.Select(u => new Backend.Application.DTOs.UserListDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username
        });
    }
}
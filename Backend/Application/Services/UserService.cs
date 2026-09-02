using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Application.Validators;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

namespace Backend.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly RegisterUserValidator _validator;

        public UserService(
            IUserRepository userRepository,
            IPasswordService passwordService,
            RegisterUserValidator validator)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _validator = validator;
        }

        public async Task<int> RegisterAsync(RegisterUserRequest request)
        {
            var errors = _validator.Validate(request);

            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    string.Join(" ", errors)
                );
            }

            var usernameExists =
                await _userRepository.ExistsByUsernameAsync(
                    request.Username
                );

            if (usernameExists)
            {
                throw new InvalidOperationException(
                    "El nombre de usuario ya está registrado."
                );
            }

            var ciExists =
                await _userRepository.ExistsByCIAsync(
                    request.CI,
                    request.Complement
                );

            if (ciExists)
            {
                throw new InvalidOperationException(
                    "Ya existe un usuario registrado con ese CI y complemento."
                );
            }

            var roleId =
                await _userRepository.GetRoleIdByNameAsync("User");

            if (roleId == null)
            {
                throw new InvalidOperationException(
                    "El rol de usuario no está configurado."
                );
            }

            var user = new User
            {
                CI = request.CI,
                Complement = request.Complement,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone?.Trim(),
                Username = request.Username.Trim(),
                RoleId = roleId.Value,
                Status = "Active",
                IsActive = true
            };

            user.PasswordHash =
                _passwordService.HashPassword(
                    user,
                    request.Password
                );

            return await _userRepository.CreateAsync(user);
        }
    }
}
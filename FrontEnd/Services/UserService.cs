using System.Net.Http.Json;
using FrontEnd.DTOs;

namespace FrontEnd.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<RegisterUserResponseDto> RegisterAsync(
            RegisterUserDto user)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada."
                );
            }

            var url = $"{backendUrl}/api/users/register";

            var response =
                await _httpClient.PostAsJsonAsync(url, user);

            var result =
                await response.Content
                    .ReadFromJsonAsync<RegisterUserResponseDto>();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    result?.Message ?? "No se pudo registrar el usuario."
                );
            }

            return result ?? new RegisterUserResponseDto
            {
                Message = "Usuario registrado correctamente."
            };
        }
    }
}
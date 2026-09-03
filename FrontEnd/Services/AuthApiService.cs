using FrontEnd.DTOs;
using System.Net.Http.Json;

namespace FrontEnd.Services;

public class AuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AuthApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(
        RegisterUserDto user)
    {
        var backendUrl = GetBackendUrl();

        var response = await _httpClient.PostAsJsonAsync(
            $"{backendUrl}/api/users/register",
            user
        );

        if (response.IsSuccessStatusCode)
        {
            return (
                true,
                "Usuario registrado correctamente."
            );
        }

        var error = await response.Content
            .ReadFromJsonAsync<ApiMessageResponse>();

        return (
            false,
            error?.Message ?? "No se pudo registrar el usuario."
        );
    }

    public async Task<AuthResponseDto?> LoginAsync(
        LoginDto login)
    {
        var backendUrl = GetBackendUrl();

        var response = await _httpClient.PostAsJsonAsync(
            $"{backendUrl}/api/auth/login",
            login
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();
    }

    private string GetBackendUrl()
    {
        var backendUrl = _configuration["BackendUrl"];

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            throw new InvalidOperationException(
                "La URL del Backend no está configurada."
            );
        }

        return backendUrl.TrimEnd('/');
    }

    private class ApiMessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
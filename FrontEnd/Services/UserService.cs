using FrontEnd.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FrontEnd.Services;

public class UserService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private void AddAuthorizationHeader()
    {
        var token = _httpContextAccessor.HttpContext?.Session?.GetString("AuthToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrWhiteSpace(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<UserListDto>> GetReadersAsync()
    {
        AddAuthorizationHeader();

        try
        {
            var resp = await _httpClient.GetAsync("api/users/readers");
            if (!resp.IsSuccessStatusCode) return new List<UserListDto>();
            return await resp.Content.ReadFromJsonAsync<List<UserListDto>>() ?? new List<UserListDto>();
        }
        catch
        {
            return new List<UserListDto>();
        }
    }
}

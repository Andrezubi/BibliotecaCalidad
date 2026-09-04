using FrontEnd.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FrontEnd.Services;

public class BookService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private void AddAuthorizationHeader()
    {
        var token = _httpContextAccessor
            .HttpContext?
            .Session
            .GetString("AuthToken");

        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ======================================================
    // GET ALL
    // ======================================================

    public async Task<List<BookDto>> GetAllAsync()
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync("api/Book");

        if (!response.IsSuccessStatusCode)
        {
            return new List<BookDto>();
        }

        var books = await response.Content
            .ReadFromJsonAsync<List<BookDto>>();

        return books ?? new List<BookDto>();
    }

    // ======================================================
    // GET BY ID
    // ======================================================

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.GetAsync($"api/Book/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<BookDto>();
    }

    // ======================================================
    // CREATE
    // ======================================================

    public async Task<bool> CreateAsync(CreateBookDto book)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.PostAsJsonAsync(
            "api/Book",
            book);

        return response.IsSuccessStatusCode;
    }

    // ======================================================
    // UPDATE
    // ======================================================

    public async Task<bool> UpdateAsync(
        int id,
        UpdateBookDto book)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.PutAsJsonAsync(
            $"api/Book/{id}",
            book);

        return response.IsSuccessStatusCode;
    }

    // ======================================================
    // DELETE LÓGICO
    // ======================================================

    public async Task<bool> DeleteAsync(int id)
    {
        AddAuthorizationHeader();

        var response = await _httpClient.DeleteAsync(
            $"api/Book/{id}");

        return response.IsSuccessStatusCode;
    }
}
using FrontEnd.DTOs;

namespace FrontEnd.Services;

public class BookService
{
    private readonly HttpClient _httpClient;

    public BookService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BookDto>> GetBooksAsync(bool onlyAvailable = false)
    {
        var url = $"api/Book{(onlyAvailable ? "?onlyAvailable=true" : "")}";
        var response = await _httpClient.GetFromJsonAsync<List<BookDto>>(url);
        return response ?? new List<BookDto>();
    }
}
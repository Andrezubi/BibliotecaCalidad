using FrontEnd.DTOs;
using System.Net.Http.Json;

namespace FrontEnd.Services;

public class LoanService
{
    private readonly HttpClient _httpClient;

    public LoanService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<LoanedBookDto>> GetLoanedBooksAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<LoanedBookDto>>("api/Loans/loaned-books");
        return response ?? new List<LoanedBookDto>();
    }

    public async Task<bool> ReturnBookAsync(int copyId)
    {
        var response = await _httpClient.PostAsync($"api/Loans/{copyId}/return", null);
        return response.IsSuccessStatusCode;
    }
}
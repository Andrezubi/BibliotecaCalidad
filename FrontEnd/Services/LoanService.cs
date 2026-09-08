using FrontEnd.DTOs;

namespace FrontEnd.Services
{
    public class LoanService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LoanService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<LoanedBookDto>> GetLoanedBooksAsync()
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada."
                );
            }

            var url = $"{backendUrl}/api/loans/loaned-books";

            var books =
                await _httpClient.GetFromJsonAsync<List<LoanedBookDto>>(url);

            return books ?? new List<LoanedBookDto>();
        }
        public async Task<bool> RegisterReturnAsync(int copyId)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/return/{copyId}";

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
        public async Task<bool> RegisterLoanAsync(int bookId)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/loan-book/{bookId}";

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
    }
}
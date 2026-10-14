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

        public async Task<FineReceiptDto?> RegisterReturnWithFineAsync(uint copyId)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/return/{copyId}";

            var response = await _httpClient.PostAsync(url, null);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<FineReceiptDto>();
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

        public async Task<List<LoanListDto>> GetAllLoansAsync()
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans";

            var loans =
                await _httpClient.GetFromJsonAsync<List<LoanListDto>>(url);

            return loans ?? new List<LoanListDto>();
        }

        public async Task<LoanDto?> GetLoanByIdAsync(int id)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/{id}";

            var loan =
                await _httpClient.GetFromJsonAsync<LoanDto>(url);

            return loan;
        }
        public async Task<bool> UpdateLoanAsync(int id, UpdateLoanDto dto)
        {
            var backendUrl = _configuration["BackendUrl"];
            var url = $"{backendUrl}/api/loans/{id}";
            var response = await _httpClient.PutAsJsonAsync(url, dto);
            return response.IsSuccessStatusCode;
        }
    }
}
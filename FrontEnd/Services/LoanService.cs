using FrontEnd.DTOs;
using System.Text.Json;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace FrontEnd.Services
{
    public class LoanService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoanService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
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

            AddAuthorizationHeader();

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
        public async Task<bool> RegisterLoanAsync(int bookId, int borrowerId = 1)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/loan-book/{bookId}?borrowerId={borrowerId}";

            AddAuthorizationHeader();

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session?.GetString("AuthToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<(bool Success, string? Message, int? LoanId)> CreateLoanAsync(int borrowerId, List<int> bookIds, int? librarianId = null, int loanDays = 14)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrWhiteSpace(backendUrl))
                throw new InvalidOperationException("La URL del backend no está configurada.");

            AddAuthorizationHeader();

            var req = new
            {
                BorrowerId = (uint)borrowerId,
                BookIds = bookIds.Select(b => (uint)b).ToList(),
                LibrarianId = librarianId.HasValue ? (uint?)librarianId.Value : null,
                LoanDays = loanDays
            };

            var response = await _httpClient.PostAsJsonAsync("api/loans", req);

            if (!response.IsSuccessStatusCode)
            {
                var txt = await response.Content.ReadAsStringAsync();
                return (false, txt, null);
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                int? loanId = null;
                if (root.TryGetProperty("loanId", out var loanIdProp) && loanIdProp.ValueKind == JsonValueKind.Number)
                    loanId = loanIdProp.GetInt32();

                string? message = null;
                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    message = msgProp.GetString();

                return (true, message ?? "Préstamo creado", loanId);
            }
            catch
            {
                return (true, "Préstamo creado", null);
            }
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
    }
}
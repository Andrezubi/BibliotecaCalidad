
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
                    "La URL del backend no está configurada.");
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

        // Registrar una devolución y obtener información de la multa.
        // confirm=false -> only compute fine details without persisting return
        public async Task<FineReceiptDto?> RegisterReturnWithFineAsync(
            uint copyId, bool confirm = false)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/return/{copyId}?confirm={confirm.ToString().ToLowerInvariant()}";

            AddAuthorizationHeader();

            var response = await _httpClient.PostAsync(url, null);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<FineReceiptDto>();
        }

        // Registrar un préstamo individual.
        public async Task<bool> RegisterLoanAsync(
            int bookId,
            int borrowerId = 1)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url =
                $"{backendUrl}/api/loans/loan-book/{bookId}?borrowerId={borrowerId}";

            AddAuthorizationHeader();

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }

        // Agregar el token de autenticación a las peticiones.
        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?
                .Session?.GetString("AuthToken");

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Registrar un préstamo con varios libros para un usuario.
        public async Task<(bool Success, string? Message, int? LoanId)>
            CreateLoanAsync(
                int borrowerId,
                List<int> bookIds,
                int? librarianId = null,
                int loanDays = 14)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrWhiteSpace(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            if (bookIds == null || bookIds.Count == 0)
            {
                return (false, "Debes seleccionar al menos un libro.", null);
            }

            AddAuthorizationHeader();

            var req = new
            {
                BorrowerId = (uint)borrowerId,
                BookIds = bookIds.Select(b => (uint)b).ToList(),
                LibrarianId = librarianId.HasValue
                    ? (uint?)librarianId.Value
                    : null,
                LoanDays = loanDays
            };

            var url = $"{backendUrl}/api/loans";

            var response = await _httpClient.PostAsJsonAsync(url, req);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return (false, error, null);
            }

            var body = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int? loanId = null;

                if (root.TryGetProperty("loanId", out var loanIdProp)
                    && loanIdProp.ValueKind == JsonValueKind.Number)
                {
                    loanId = loanIdProp.GetInt32();
                }

                string? message = null;

                if (root.TryGetProperty("message", out var msgProp)
                    && msgProp.ValueKind == JsonValueKind.String)
                {
                    message = msgProp.GetString();
                }

                return (
                    true,
                    message ?? "Préstamo creado.",
                    loanId
                );
            }
            catch (JsonException)
            {
                return (true, "Préstamo creado.", null);
            }
        }

        // Obtener todos los préstamos.
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

        // Obtener un préstamo por su ID.
        public async Task<LoanDto?> GetLoanByIdAsync(int id)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrEmpty(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/{id}";

            return await _httpClient.GetFromJsonAsync<LoanDto>(url);
        }

        // Actualizar un préstamo.
        public async Task<bool> UpdateLoanAsync(
            int id,
            UpdateLoanDto dto)
        {
            var backendUrl = _configuration["BackendUrl"];

            if (string.IsNullOrWhiteSpace(backendUrl))
            {
                throw new InvalidOperationException(
                    "La URL del backend no está configurada.");
            }

            var url = $"{backendUrl}/api/loans/{id}";

            AddAuthorizationHeader();

            var response = await _httpClient.PutAsJsonAsync(url, dto);

            return response.IsSuccessStatusCode;
        }
    }
}
